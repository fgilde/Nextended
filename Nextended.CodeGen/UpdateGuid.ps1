param(
  [Parameter(Mandatory=$true)][string]$File,          # z.B. ...\Generator.cs
  [string]$FieldName = 'BuildId'
)

$guid = [guid]::NewGuid().ToString()
$text = Get-Content -LiteralPath $File -Raw
$escapedField = [regex]::Escape($FieldName)

# Muster mit EINEM Anführungszeichen
$patterns = @(
  ('(?is)(' + "\b$escapedField\s*=\s*new\s+(?:System\.)?Guid\s*\(\s*""" + ')[0-9A-Fa-f-]{36}(' + '"\s*\)\s*;)'),
  ('(?is)(' + "\b$escapedField\s*=\s*Guid\s*\.\s*Parse\s*\(\s*"""      + ')[0-9A-Fa-f-]{36}(' + '"\s*\)\s*;)'),
  ('(?is)(' + "\b$escapedField\s*=\s*new\s*\(\s*"""                    + ')[0-9A-Fa-f-]{36}(' + '"\s*\)\s*;)')
)

$replaced = $false
foreach ($pattern in $patterns) {
  $newText = [regex]::Replace(
    $text,
    $pattern,
    { param($m) $m.Groups[1].Value + $guid + $m.Groups[2].Value },
    [System.Text.RegularExpressions.RegexOptions]::Singleline
  )
  if ($newText -ne $text) { $text = $newText; $replaced = $true; break }
}

if (-not $replaced) {
  Write-Error "Guid-Feld '$FieldName' nicht gefunden in $File."
  # Kontext ausgeben:
  $lines = (Get-Content -LiteralPath $File)
  $idx = ($lines | Select-String -SimpleMatch $FieldName | Select-Object -First 1).LineNumber
  if ($idx) {
    $start = [Math]::Max(1, $idx-2); $end = [Math]::Min($lines.Count, $idx+2)
    Write-Host "Kontext (Zeilen $start..$end):"
    for ($i=$start; $i -le $end; $i++) { Write-Host ("{0,4}: {1}" -f $i, $lines[$i-1]) }
  }
  exit 1
}

Set-Content -LiteralPath $File -Value $text -Encoding UTF8
