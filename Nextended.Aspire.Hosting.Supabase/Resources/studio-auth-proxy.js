// Login in front of Supabase Studio, which has none of its own. Studio runs on PORT+1, this proxy
// on PORT and lets a request through only with a login:
//  * basic (DASHBOARD_USERNAME / DASHBOARD_PASSWORD): HTTP Basic Auth;
//  * oauth (STUDIO_OAUTH_*): OpenID Connect / OAuth 2.1 sign-in with PKCE at a provider — Supabase
//    Auth of the same stack, Keycloak … —, admitted only if the token carries the required claim
//    value; then a signed session cookie for STUDIO_OAUTH_SESSION_HOURS.
'use strict';
const http = require('http');
const crypto = require('crypto');
const { spawn } = require('child_process');

const env = process.env;
const PORT = parseInt(env.PORT || '3000', 10);
const STUDIO_PORT = PORT + 1;
const MODE = env.STUDIO_AUTH_MODE === 'oauth' ? 'oauth' : 'basic';
const CALLBACK_PATH = '/__oauth/callback';
const SESSION_COOKIE = 'studio_session';
const STATE_COOKIE = 'studio_oauth';

const b64url = (buf) => Buffer.from(buf).toString('base64url');
const fromB64url = (text) => Buffer.from(text, 'base64url').toString('utf8');

function cookies(req) {
  const out = {};
  for (const part of (req.headers.cookie || '').split(';')) {
    const i = part.indexOf('=');
    if (i > 0) out[part.slice(0, i).trim()] = decodeURIComponent(part.slice(i + 1).trim());
  }
  return out;
}

// ---- signed values (session, PKCE state) --------------------------------------------------
const SECRET = crypto.createHash('sha256')
  .update('studio-auth-proxy:' + (env.STUDIO_OAUTH_SESSION_SECRET || env.AUTH_JWT_SECRET || crypto.randomBytes(32).toString('hex')))
  .digest();
const sign = (payload) => {
  const body = b64url(JSON.stringify(payload));
  return body + '.' + b64url(crypto.createHmac('sha256', SECRET).update(body).digest());
};
function verify(value) {
  if (!value || value.indexOf('.') < 0) return null;
  const [body, mac] = value.split('.');
  const expected = b64url(crypto.createHmac('sha256', SECRET).update(body).digest());
  if (mac.length !== expected.length || !crypto.timingSafeEqual(Buffer.from(mac), Buffer.from(expected))) return null;
  try {
    const payload = JSON.parse(fromB64url(body));
    return payload.exp && payload.exp * 1000 > Date.now() ? payload : null;
  } catch { return null; }
}

// ---- OAuth -----------------------------------------------------------------------------------
const oauth = {
  authUrl: env.STUDIO_OAUTH_AUTH_URL,
  tokenUrl: env.STUDIO_OAUTH_TOKEN_URL,
  clientId: env.STUDIO_OAUTH_CLIENT_ID,
  clientSecret: env.STUDIO_OAUTH_CLIENT_SECRET || '',
  scopes: env.STUDIO_OAUTH_SCOPES || 'openid email profile',
  claim: env.STUDIO_OAUTH_CLAIM || '',
  value: env.STUDIO_OAUTH_VALUE || '',
  redirectUri: env.STUDIO_OAUTH_REDIRECT_URI || '',
  sessionHours: parseFloat(env.STUDIO_OAUTH_SESSION_HOURS || '8'),
};

function redirectUriFor(req) {
  if (oauth.redirectUri) return oauth.redirectUri;
  const proto = (req.headers['x-forwarded-proto'] || 'http').split(',')[0].trim();
  const host = req.headers['x-forwarded-host'] || req.headers.host;
  return `${proto}://${host}${CALLBACK_PATH}`;
}

const secure = (req) => redirectUriFor(req).startsWith('https://');
function setCookie(req, name, value, maxAgeSeconds) {
  return `${name}=${encodeURIComponent(value)}; Path=/; HttpOnly; SameSite=Lax; Max-Age=${maxAgeSeconds}` + (secure(req) ? '; Secure' : '');
}

// The token came straight from the provider over the back channel, so its claims are taken as they
// are; the ID token if there is one, else the access token (Supabase Auth with an HS256 secret).
function claimsOf(token) {
  const jwt = token.id_token || token.access_token;
  if (!jwt || jwt.split('.').length !== 3) return null;
  try { return JSON.parse(fromB64url(jwt.split('.')[1])); } catch { return null; }
}

// "permissions" or "realm_access.roles": the value itself or one entry of an array.
function hasRequired(claims) {
  if (!oauth.claim) return true;
  let node = claims;
  for (const key of oauth.claim.split('.')) node = node == null ? undefined : node[key];
  if (Array.isArray(node)) return node.map(String).includes(oauth.value);
  return node != null && String(node) === oauth.value;
}

function startSignIn(req, res) {
  const verifier = b64url(crypto.randomBytes(32));
  const state = b64url(crypto.randomBytes(16));
  const returnTo = req.url && req.url.startsWith('/') && !req.url.startsWith('//') ? req.url : '/';
  const target = new URL(oauth.authUrl);
  target.searchParams.set('response_type', 'code');
  target.searchParams.set('client_id', oauth.clientId);
  target.searchParams.set('redirect_uri', redirectUriFor(req));
  target.searchParams.set('scope', oauth.scopes);
  target.searchParams.set('state', state);
  target.searchParams.set('code_challenge', b64url(crypto.createHash('sha256').update(verifier).digest()));
  target.searchParams.set('code_challenge_method', 'S256');
  res.writeHead(302, {
    Location: target.toString(),
    'Set-Cookie': setCookie(req, STATE_COOKIE, sign({ state, verifier, returnTo, exp: Math.floor(Date.now() / 1000) + 600 }), 600),
  });
  res.end();
}

function deny(res, status, text) {
  res.writeHead(status, { 'Content-Type': 'text/plain; charset=utf-8' });
  res.end(text);
}

async function finishSignIn(req, res, url) {
  const pending = verify(cookies(req)[STATE_COOKIE]);
  const error = url.searchParams.get('error');
  if (error) return deny(res, 403, `Sign-in refused: ${url.searchParams.get('error_description') || error}`);
  if (!pending || pending.state !== url.searchParams.get('state')) return deny(res, 400, 'Sign-in expired or not started here — open Studio again.');

  const form = new URLSearchParams({
    grant_type: 'authorization_code',
    code: url.searchParams.get('code') || '',
    redirect_uri: redirectUriFor(req),
    client_id: oauth.clientId,
    code_verifier: pending.verifier,
  });
  if (oauth.clientSecret) form.set('client_secret', oauth.clientSecret);
  let token;
  try {
    const response = await fetch(oauth.tokenUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded', Accept: 'application/json' },
      body: form,
    });
    token = await response.json();
    if (!response.ok) return deny(res, 403, `Sign-in refused: ${token.error_description || token.error || response.status}`);
  } catch (e) {
    console.error('[Auth Proxy] token request failed:', e.message);
    return deny(res, 502, 'The sign-in provider is not reachable.');
  }

  const claims = claimsOf(token);
  if (!claims) return deny(res, 403, 'The sign-in provider sent no readable token.');
  if (!hasRequired(claims)) return deny(res, 403, 'No access to Supabase Studio for this account.');

  const exp = Math.floor(Date.now() / 1000 + oauth.sessionHours * 3600);
  res.writeHead(302, {
    Location: pending.returnTo || '/',
    'Set-Cookie': [
      setCookie(req, SESSION_COOKIE, sign({ sub: claims.sub, email: claims.email, exp }), Math.floor(oauth.sessionHours * 3600)),
      setCookie(req, STATE_COOKIE, '', 0),
    ],
  });
  res.end();
}

// ---- gate + proxy ------------------------------------------------------------------------------
function forward(req, res) {
  const headers = { ...req.headers };
  delete headers.authorization;
  const upstream = http.request({ hostname: '127.0.0.1', port: STUDIO_PORT, path: req.url, method: req.method, headers }, (up) => {
    res.writeHead(up.statusCode, up.headers);
    up.pipe(res);
  });
  upstream.on('error', () => { res.writeHead(502); res.end('Studio not ready'); });
  req.pipe(upstream);
}

function basicGate(req, res) {
  const user = env.DASHBOARD_USERNAME || 'admin';
  const pass = env.DASHBOARD_PASSWORD || 'admin';
  const auth = req.headers.authorization;
  if (!auth || !auth.startsWith('Basic ')) {
    res.writeHead(401, { 'WWW-Authenticate': 'Basic realm="Supabase Studio"' });
    return res.end('Unauthorized');
  }
  const credentials = Buffer.from(auth.slice(6), 'base64').toString();
  const colon = credentials.indexOf(':');
  if (credentials.slice(0, colon) !== user || credentials.slice(colon + 1) !== pass) {
    res.writeHead(401, { 'WWW-Authenticate': 'Basic realm="Supabase Studio"' });
    return res.end('Invalid credentials');
  }
  forward(req, res);
}

function oauthGate(req, res) {
  const url = new URL(req.url, 'http://studio');
  if (url.pathname === CALLBACK_PATH) {
    finishSignIn(req, res, url).catch((e) => { console.error('[Auth Proxy]', e); deny(res, 500, 'Sign-in failed.'); });
    return;
  }
  if (verify(cookies(req)[SESSION_COOKIE])) return forward(req, res);
  // A page load signs in; Studio's own API calls get a 401 instead of a redirect they cannot follow.
  if (req.method === 'GET' && (req.headers.accept || '').includes('text/html')) return startSignIn(req, res);
  deny(res, 401, 'Unauthorized');
}

if (MODE === 'oauth') {
  for (const name of ['authUrl', 'tokenUrl', 'clientId']) {
    if (!oauth[name]) { console.error(`[Auth Proxy] STUDIO_OAUTH_* incomplete: ${name} missing`); process.exit(1); }
  }
}

// ---- start Studio, then the proxy ---------------------------------------------------------------
const studio = spawn('node', [env.STUDIO_SERVER_JS || 'apps/studio/server.js'], {
  cwd: env.STUDIO_APP_DIR || '/app',
  stdio: 'inherit',
  env: { ...env, PORT: String(STUDIO_PORT), HOSTNAME: '0.0.0.0' },
});
studio.on('exit', (code) => process.exit(code || 0));

setTimeout(() => {
  http.createServer(MODE === 'oauth' ? oauthGate : basicGate)
    .listen(PORT, '0.0.0.0', () => console.log(`[Auth Proxy] ${MODE} login on ${PORT}, forwarding to Studio on ${STUDIO_PORT}`));
}, parseInt(env.STUDIO_PROXY_DELAY_MS || '3000', 10));
