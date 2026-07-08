# Script theo dõi hiệu năng đỉnh (Peak Stats) của các Container Docker
# Hướng dẫn chạy:
# 1. Mở một cửa sổ PowerShell mới
# 2. cd C:\Project\TheFirstSectOrigin\server\cmd\load_test
# 3. Chạy lệnh: .\monitor_peak.ps1
# 4. Tiến hành chạy bài test CCU (100, 500, 1000, 5000 CCU) bên cửa sổ kia.
# 5. Khi bài test kết thúc, nhấn CTRL+C ở cửa sổ monitor này để nhận kết quả Peak Stats.

$peakStats = @{}

Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host "      DOCKER CONTAINER PEAK PERFORMANCE MONITOR              " -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host "Đang theo dõi... Hãy bắt đầu chạy bài test CCU ở cửa sổ khác."
Write-Host "Nhấn CTRL+C ở đây để dừng và xuất kết quả Peak Stats." -ForegroundColor Yellow
Write-Host "-------------------------------------------------------------"

# Thiết lập chế độ bắt Ctrl+C bằng cách bắt lỗi hoặc vòng lặp dừng
$Host.UI.RawUI.FlushInputBuffer()

try {
    while ($true) {
        # Lấy thông số từ docker stats (chạy 1 lần không stream)
        $stats = docker stats --no-stream --format "{{.Name}},{{.CPUPerc}},{{.MemUsage}}"
        
        foreach ($line in $stats) {
            # Format: thefirstsect_dev_gateway,12.50%,120.5MiB / 15.95GiB
            if ($line -match "^([^,]+),([0-9.]+)%,([0-9.]+)(GiB|MiB|KiB|B)\s*/\s*([0-9.]+)(GiB|MiB|KiB|B)") {
                $name = $Matches[1].Trim()
                $cpuStr = $Matches[2].Trim()
                $memValStr = $Matches[3].Trim()
                $memUnit = $Matches[4].Trim()

                # Chuyển CPU sang float
                $cpu = [float]$cpuStr

                # Chuyển RAM sang MB để so sánh tìm Peak
                $memVal = [float]$memValStr
                $memInMB = 0.0
                if ($memUnit -eq "GiB") {
                    $memInMB = $memVal * 1024
                } elseif ($memUnit -eq "MiB") {
                    $memInMB = $memVal
                } elseif ($memUnit -eq "KiB") {
                    $memInMB = $memVal / 1024
                } else {
                    $memInMB = $memVal / (1024 * 1024)
                }

                $rawMemDisplay = "$memValStr $memUnit"

                # Cập nhật Peak
                if (-not $peakStats.ContainsKey($name)) {
                    $peakStats[$name] = @{
                        "PeakCPU" = $cpu
                        "PeakMem" = $memInMB
                        "MemRaw"  = $rawMemDisplay
                    }
                } else {
                    if ($cpu -gt $peakStats[$name].PeakCPU) {
                        $peakStats[$name].PeakCPU = $cpu
                    }
                    if ($memInMB -gt $peakStats[$name].PeakMem) {
                        $peakStats[$name].PeakMem = $memInMB
                        $peakStats[$name].MemRaw = $rawMemDisplay
                    }
                }
            }
        }
        Start-Sleep -Seconds 1
    }
}
catch {
    # Bắt sự kiện dừng Ctrl+C của người dùng
}
finally {
    Write-Host "`n=============================================================" -ForegroundColor Green
    Write-Host "                 KẾT QUẢ HIỆU NĂNG ĐỈNH (PEAK STATS)          " -ForegroundColor Green
    Write-Host "=============================================================" -ForegroundColor Green
    Write-Host ("{0,-32} | {1,-14} | {2,-15}" -f "Tên Container", "Peak CPU (%)", "Peak Memory")
    Write-Host "-------------------------------------------------------------"
    
    foreach ($name in $peakStats.Keys) {
        $cpu = $peakStats[$name].PeakCPU
        $mem = $peakStats[$name].MemRaw
        Write-Host ("{0,-32} | {1,12:N2}% | {2,-15}" -f $name, $cpu, $mem)
    }
    Write-Host "=============================================================" -ForegroundColor Green
}
