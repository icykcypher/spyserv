$headers = @{
    "Origin" = "http://localhost:12345"
    "Access-Control-Request-Method" = "POST"
}

Invoke-WebRequest -Uri "http://spyserv.dev/api/u/sign-in" -Method Options -Headers $headers -Verbose
