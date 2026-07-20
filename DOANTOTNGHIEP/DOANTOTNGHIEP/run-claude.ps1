Remove-Item Env:ANTHROPIC_API_KEY -ErrorAction SilentlyContinue
$env:ANTHROPIC_BASE_URL = "http://localhost:20128"
$env:ANTHROPIC_AUTH_TOKEN = "sk-fc874087fd17af6a-jzvkf9-07a1dd20"
$env:OPENROUTER_API_KEY = "sk-fc874087fd17af6a-jzvkf9-07a1dd20"
$env:ANTHROPIC_MODEL = "backend"

claude
