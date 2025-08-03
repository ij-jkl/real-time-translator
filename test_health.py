# Health Check Test Script - Python
# This script tests all the health endpoints

import requests
import json

def check_service(name, url):
    try:
        response = requests.get(url, timeout=5)
        if response.status_code == 200:
            print(f"{name}: HEALTHY")
            print(f" Response: {json.dumps(response.json(), indent=2)}")
        else:
            print(f"{name}: UNHEALTHY (Status: {response.status_code})")
    except Exception as e:
        print(f"{name}: UNREACHABLE ({str(e)})")
    print()

def main():
    print("Health Check Report")
    print("----------------------------------------")
    
    # Test direct service endpoints
    check_service("Transcriptor Service", "http://localhost:8000/healthz")
    check_service("Audio Streaming Service", "http://localhost:9000/healthz")
    
    # Test gateway endpoints
    check_service("API Gateway Health", "http://localhost:5000/health")
    check_service("API Gateway Status", "http://localhost:5000/health/status")
    
    # Test status endpoints
    check_service("Transcriptor Status", "http://localhost:8000/status")
    check_service("Audio Streaming Status", "http://localhost:9000/status")

if __name__ == "__main__":
    main()
