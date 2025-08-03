# Real-Time Translator - Start All Services ! Isaac Jordan
# This script starts all microservices and checks their health

Write-Host "---   Starting Real-Time Translator Services...   ---" -ForegroundColor Yellow
Write-Host ""

# Function to start service in new window
function Start-Service {
    param($Name, $Path, $Command, $Color)
    Write-Host "Starting $Name..." -ForegroundColor $Color
    Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$Path'; $Command; Write-Host 'Press any key to close...'; Read-Host"
    Start-Sleep -Seconds 2
}

# Function to test endpoint
function Test-Endpoint {
    param($Name, $Url, $MaxRetries = 5)
    $retries = 0
    do {
        try {
            $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 5 -ErrorAction Stop
            Write-Host "$Name : HEALTHY" -ForegroundColor Green
            return $true
        }
        catch {
            $retries++
            if ($retries -lt $MaxRetries) {
                Write-Host "$Name : Waiting... (attempt $retries/$MaxRetries)" -ForegroundColor Yellow
                Start-Sleep -Seconds 3
            }
        }
    } while ($retries -lt $MaxRetries)
    
    Write-Host "$Name : FAILED TO START" -ForegroundColor Red
    return $false
}

# Get the current directory, should be the root of the project with all the files
$rootPath = Get-Location

# Start all services,
Write-Host "Starting Backend Services:" -ForegroundColor Cyan
Start-Service "Transcriptor Python" "$rootPath\transcriptor-python" "uvicorn main:app --host 0.0.0.0 --port 8000" "Magenta"
Start-Service "Audio Streaming" "$rootPath\audio-streaming-service" "uvicorn main:app --host 0.0.0.0 --port 9000" "Blue"
Start-Service "API Gateway" "$rootPath\api-gateway\gatewayapi" "dotnet run" "Green"

Write-Host ""
Write-Host "Starting Frontend:" -ForegroundColor Cyan
Start-Service "Angular Frontend" "$rootPath\angular-front" "ng serve" "Cyan"

Write-Host "----------------------------------------"
Write-Host "Waiting for services to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "Health Check Report:" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Yellow

# Test all services, healthz endpoints
$allHealthy = $true
$allHealthy = (Test-Endpoint "Transcriptor Service" "http://localhost:8000/healthz") -and $allHealthy
$allHealthy = (Test-Endpoint "Audio Streaming Service" "http://localhost:9000/healthz") -and $allHealthy
$allHealthy = (Test-Endpoint "API Gateway" "http://localhost:5224/health") -and $allHealthy

Write-Host ""
if ($allHealthy) {
    Write-Host "ALL SERVICES STARTED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Access Points:" -ForegroundColor Cyan
    Write-Host "   Main App: http://localhost:4200" -ForegroundColor White
    Write-Host "   API Gateway: http://localhost:5224" -ForegroundColor White
    Write-Host "   Transcriptor API: http://localhost:8000" -ForegroundColor White
    Write-Host "   Audio Streaming: http://localhost:9000" -ForegroundColor White
    Write-Host ""
    Write-Host "Press Enter on this console to open the main application..."
    Read-Host
    Start-Process "http://localhost:4200"
} else {
    Write-Host "Some services failed to start. Check the individual windows for errors." -ForegroundColor Red
}

Write-Host ""
Write-Host "Note: All services are running in separate windows. Close them individually to stop the services." -ForegroundColor Gray
