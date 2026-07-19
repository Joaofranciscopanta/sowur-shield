## Example: Personalized Discord Bot Configuration

Replace lines 439-470 in the workflow with this customized version:

```yaml
DISCORD_PAYLOAD=$(cat <<EOF
{
  "username": "🌾 Sowur Shield Bot",
  "avatar_url": "YOUR_GAME_ICON_URL_HERE",
  "embeds": [{
    "title": "🚀 New Build Deployed!",
    "description": "A fresh Sowur Shield build is now live and ready to play!",
    "color": 5763719,
    "thumbnail": {
      "url": "YOUR_GAME_ICON_URL_HERE"
    },
    "fields": [
      {
        "name": "🔢 Build Number",
        "value": "#$BUILD_NUM",
        "inline": true
      },
      {
        "name": "📅 Deployed",
        "value": "$DEPLOY_DATE",
        "inline": true
      },
      {
        "name": "👨‍💻 Team",
        "value": "João & Lucas",
        "inline": true
      },
      {
        "name": "🎮 Play Now",
        "value": "[Launch Demo](https://joaofranciscopanta.github.io/sowur-shield/)",
        "inline": false
      },
      {
        "name": "📂 Resources",
        "value": "[GitHub](https://github.com/Joaofranciscopanta/sowur-shield) • [Workflow Logs](${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }})",
        "inline": false
      }
    ],
    "footer": {
      "text": "🌾 Made with ❤️ by Sowur Shield Team • Automated by GitHub Actions",
      "icon_url": "https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png"
    },
    "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
  }]
}
EOF
)
```

### Key Changes in This Example:
- 🌾 emoji in bot name
- Custom green color (5763719)
- Added thumbnail for visual appeal
- Added "Team" field
- Separated links into two fields for better readability
- Custom footer with emoji and icon
- More engaging language

### For Failure Notifications:
Apply similar changes to lines 493-520 (failure notification section).
