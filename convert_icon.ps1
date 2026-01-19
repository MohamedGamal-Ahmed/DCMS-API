Add-Type -AssemblyName System.Drawing

# Use relative paths to avoid encoding issues with Arabic characters in absolute path
$currentDir = Get-Location
$pngPath = Join-Path $currentDir "src\DCMS.WPF\Assets\dcms_logo.png"
$icoPath = Join-Path $currentDir "src\DCMS.WPF\Assets\dcms_logo.ico"

try {
    Write-Host "Converting $pngPath to $icoPath..."
    
    if (-not (Test-Path $pngPath)) {
        throw "Source file not found: $pngPath"
    }

    $bitmap = [System.Drawing.Bitmap]::FromFile($pngPath)
    $handle = $bitmap.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($handle)
    
    $file = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
    $icon.Save($file)
    $file.Close()
    
    # Cleanup
    # [System.Runtime.InteropServices.Marshal]::DestroyIcon($handle) | Out-Null # Not needed/available in this context
    $icon.Dispose()
    $bitmap.Dispose()
    
    Write-Host "Conversion successful!"
}
catch {
    Write-Error "Conversion failed: $_"
    exit 1
}
