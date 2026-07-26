<?php
// Demo dashboard

$mysql = ['ok' => false, 'detail' => 'MYSQL_URL not set', 'databases' => []];
$url = parse_url(getenv('MYSQL_URL') ?: '');
if (!empty($url['host'])) {
    if (!extension_loaded('mysqli')) {
        $mysql['detail'] = 'mysqli extension not loaded — add WithPhpExtensions("mysqli") in the AppHost';
    } else {
        mysqli_report(MYSQLI_REPORT_OFF);
        $conn = @mysqli_connect($url['host'], getenv('MYSQL_USER') ?: 'root', getenv('MYSQL_PASSWORD') ?: '', '', $url['port'] ?? 3306);
        if ($conn) {
            $mysql = [
                'ok'        => true,
                'detail'    => 'MySQL ' . mysqli_get_server_info($conn) . ' @ ' . $url['host'] . ':' . ($url['port'] ?? 3306),
                'databases' => array_column(mysqli_fetch_all(mysqli_query($conn, 'SHOW DATABASES'), MYSQLI_ASSOC), 'Database'),
            ];
            mysqli_close($conn);
        } else {
            $mysql['detail'] = 'Connection failed: ' . mysqli_connect_error();
        }
    }
}
$pma = getenv('PHPMYADMIN_URL') ?: null;

function badge(bool $ok): string {
    return $ok ? '<span class="badge ok">connected</span>' : '<span class="badge err">offline</span>';
}
?><!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>PHP in Aspire — demo</title>
<style>
  :root { color-scheme: light dark; }
  body { font-family: system-ui, sans-serif; margin: 0; min-height: 100vh; padding: 3rem 1.5rem;
         background: light-dark(#f5f6fa, #14161c); color: light-dark(#1c1e26, #e8eaf0); box-sizing: border-box; }
  main { max-width: 62rem; margin: 0 auto; }
  h1 { margin: 0 0 .3rem; font-size: 1.7rem; }
  .sub { opacity: .65; margin-bottom: 2rem; }
  .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr)); gap: 1rem; }
  .card { background: light-dark(#fff, #1e222c); border: 1px solid light-dark(#e3e5ec, #2c3140);
          border-radius: .75rem; padding: 1.1rem 1.25rem; box-shadow: 0 1px 3px rgb(0 0 0 / .06); }
  .card h2 { margin: 0 0 .6rem; font-size: 1.02rem; display: flex; align-items: center; gap: .5rem; }
  .card p { margin: .3rem 0; font-size: .9rem; opacity: .85; overflow-wrap: anywhere; }
  .badge { font-size: .72rem; font-weight: 600; padding: .15rem .55rem; border-radius: 1rem; }
  .badge.ok  { background: #d9f2e1; color: #0a7d32; }
  .badge.err { background: #fbdcda; color: #b3261e; }
  ul { margin: .4rem 0 0; padding-left: 1.2rem; font-size: .85rem; opacity: .85; }
  a.btn { display: inline-block; margin-top: .6rem; padding: .45rem 1rem; border-radius: .5rem;
          background: #4059ad; color: #fff; text-decoration: none; font-size: .88rem; }
  a.btn:hover { background: #32478c; }
  code { background: light-dark(#eef0f6, #2a2f3c); padding: .1rem .35rem; border-radius: .3rem; font-size: .85em; }
  footer { margin-top: 2rem; font-size: .8rem; opacity: .55; }
</style>
</head>
<body>
<main>
  <h1>🐘 PHP in Aspire</h1>
  <p class="sub">Served by <code>Nextended.Aspire.Hosting.Php</code> — PHP's built-in server in the official <code>php:cli</code> container.</p>

  <div class="grid">
    <div class="card">
      <h2>⚙️ PHP <span class="badge ok">running</span></h2>
      <p>Version <?= htmlspecialchars(PHP_VERSION) ?></p>
      <p><code>memory_limit</code> = <?= htmlspecialchars(ini_get('memory_limit')) ?>,
         <code>date.timezone</code> = <?= htmlspecialchars(ini_get('date.timezone') ?: '(unset)') ?>
         — via <code>WithPhpIniConfiguration</code></p>
      <a class="btn" href="/phpinfo.php">phpinfo()</a>
    </div>

    <div class="card">
      <h2>🐬 MySQL <?= badge($mysql['ok']) ?></h2>
      <p><?= htmlspecialchars($mysql['detail']) ?></p>
      <?php if ($mysql['databases']): ?>
        <ul><?php foreach ($mysql['databases'] as $db): ?><li><?= htmlspecialchars($db) ?></li><?php endforeach; ?></ul>
      <?php endif; ?>
    </div>

    <div class="card">
      <h2>🛠️ phpMyAdmin</h2>
      <?php if ($pma): ?>
        <p>Browse the <code>mysql</code> resource with phpMyAdmin (Aspire's built-in <code>WithPhpMyAdmin()</code>).</p>
        <a class="btn" href="<?= htmlspecialchars($pma) ?>" target="_blank">Open phpMyAdmin</a>
      <?php else: ?>
        <p>PHPMYADMIN_URL not set.</p>
      <?php endif; ?>
    </div>

    <div class="card">
      <h2>📨 Mail</h2>
      <p>POST JSON to <code>/send-mail.php</code> — delivered over SMTP to the <code>mailpit</code> resource
         (open its dashboard endpoint to see the inbox), or use the <code>webdemo</code> form.</p>
    </div>
  </div>

  <footer>Generated <?= htmlspecialchars(date('c')) ?> · <?= htmlspecialchars($_SERVER['SERVER_SOFTWARE'] ?? 'php') ?></footer>
</main>
</body>
</html>
