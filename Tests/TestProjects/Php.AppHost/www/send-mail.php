<?php

header('Content-Type: application/json');

function fail(int $code, string $msg): never {
    http_response_code($code);
    echo json_encode(['error' => $msg]);
    exit;
}

if ($_SERVER['REQUEST_METHOD'] !== 'POST') fail(405, 'POST only');

$input = json_decode(file_get_contents('php://input'), true) ?? $_POST;
$to = trim($input['to'] ?? '');
if (!filter_var($to, FILTER_VALIDATE_EMAIL)) fail(400, 'missing or invalid "to"');
$subject = $input['subject'] ?? '(no subject)';
$body = $input['body'] ?? '';

$smtp = parse_url(getenv('SMTP_URL') ?: 'tcp://localhost:1025');
$host = $smtp['host'] ?? 'localhost';
$port = $smtp['port'] ?? 1025;

$sock = @fsockopen($host, $port, $errno, $err, 5);
if (!$sock) fail(502, "SMTP server $host:$port unreachable: $err");


$read = function () use ($sock): string {
    do { $line = fgets($sock, 512); } while ($line !== false && isset($line[3]) && $line[3] === '-');
    return $line ?: '';
};
$cmd = function (string $c, string $expect) use ($sock, $read): void {
    fwrite($sock, $c . "\r\n");
    $resp = $read();
    if (!str_starts_with($resp, $expect)) fail(502, trim("SMTP error after '$c': $resp"));
};

$read(); 
$cmd('EHLO php', '250');
$cmd('MAIL FROM:<noreply@php.local>', '250');
$cmd("RCPT TO:<$to>", '250');
$cmd('DATA', '354');
$headers = "From: noreply@php.local\r\n"
         . "To: $to\r\n"
         . "Subject: =?UTF-8?B?" . base64_encode($subject) . "?=\r\n"
         . "MIME-Version: 1.0\r\n"
         . "Content-Type: text/plain; charset=UTF-8\r\n";
$body = preg_replace('/^\./m', '..', $body); 
$cmd($headers . "\r\n" . $body . "\r\n.", '250');
fwrite($sock, "QUIT\r\n");
fclose($sock);

echo json_encode([
    'sent'    => true,
    'to'      => $to,
    'subject' => $subject,
    'via'     => "$host:$port",
    'php'     => PHP_VERSION,
]);
