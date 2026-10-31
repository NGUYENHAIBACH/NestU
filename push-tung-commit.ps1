# Đẩy lên GitHub TỪNG COMMIT MỘT, cách nhau vài giây, để lịch sử trên GitHub lên dần thay vì một cục.
# Chạy trong PowerShell tại thư mục NestU:   .\push-tung-commit.ps1
# Tham số:  -Remote (mặc định origin)  -Branch (mặc định main)  -DelaySeconds (mặc định 20)
param([string]$Remote = "origin", [string]$Branch = "main", [int]$DelaySeconds = 20)
$ErrorActionPreference = "Stop"

if (-not (git remote | Select-String -SimpleMatch $Remote)) {
    git remote add $Remote https://github.com/NGUYENHAIBACH/NestU.git
}

# Các commit từ cũ tới mới chưa có trên remote
git fetch $Remote $Branch 2>$null
$remoteHead = git rev-parse --verify --quiet "$Remote/$Branch"
$range = if ($remoteHead) { "$remoteHead..$Branch" } else { $Branch }
$commits = git rev-list --reverse $range
if (-not $commits) { Write-Host "Không có commit mới để đẩy." -ForegroundColor Green; exit 0 }

$i = 0; $n = ($commits | Measure-Object).Count
foreach ($c in $commits) {
    $i++
    $msg = git log -1 --format=%s $c
    Write-Host ("[{0}/{1}] {2}" -f $i, $n, $msg) -ForegroundColor Cyan
    git push $Remote "${c}:refs/heads/$Branch" --quiet
    if ($i -lt $n) { Start-Sleep -Seconds $DelaySeconds }
}
git branch --set-upstream-to="$Remote/$Branch" $Branch | Out-Null
Write-Host "Đã đẩy xong $n commit." -ForegroundColor Green
