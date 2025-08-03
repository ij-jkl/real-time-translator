# Quick Health Check Script - To check by themselves

Write-Host "Checking Service Health..." -ForegroundColor Yellow

# Function to test endpoint
function Test-Endpoint {
    param($Name, $Url)
    try {
        $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 5
        Write-Host "$Name : HEALTHY" -ForegroundColor Green
        Write-Host " Response: $($response | ConvertTo-Json -Compress)" -ForegroundColor Gray
    }
    catch {
        Write-Host "$Name : UNREACHABLE" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
    Write-Host ""
}

Write-Host "Testing Health Endpoints:" -ForegroundColor Cyan

Test-Endpoint "Transcriptor Service" "http://localhost:8000/healthz"
Test-Endpoint "Audio Streaming Service" "http://localhost:9000/healthz"

Test-Endpoint "API Gateway Health" "http://localhost:5224/health"
Test-Endpoint "API Gateway Status" "http://localhost:5224/health/status"

Write-Host "Testing Status Endpoints:" -ForegroundColor Cyan

Test-Endpoint "Transcriptor Status" "http://localhost:8000/status"
Test-Endpoint "Audio Streaming Status" "http://localhost:9000/status"

Write-Host "Health check complete!" -ForegroundColor Yellow
