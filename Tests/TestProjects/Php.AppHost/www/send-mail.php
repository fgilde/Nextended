<?php
// Demo "send mail" endpoint: POST {"to": "...", "subject": "...", "body": "..."}.
// php:cli ships no MTA, so mail() would silently no-op here — wire up SMTP (e.g. PHPMailer)
// or a custom image with msmtp for real sending; this echoes the accepted payload.
header('Content-Type: application/json');

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['error' => 'POST only']);
    exit;
}

$input = json_decode(file_get_contents('php://input'), true) ?? $_POST;
if (empty($input['to'])) {
    http_response_code(400);
    echo json_encode(['error' => 'missing "to"']);
    exit;
}

echo json_encode([
    'accepted' => true,
    'to'       => $input['to'],
    'subject'  => $input['subject'] ?? '(none)',
]);
