@echo off
echo Starting cloudflared tunnel to local API (http://localhost:8080)...
echo Keep this window open. Copy the https://...trycloudflare.com URL below.
echo Open on phone: https://m1nnesang.github.io/RouteOptimizer/?api=^<that-url^>
echo.
"C:\Program Files (x86)\cloudflared\cloudflared.exe" tunnel --url http://localhost:8080
pause
