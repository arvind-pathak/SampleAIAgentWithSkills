---
name: greeting
description: "A skill that responds to greetings and help requests with a cheerful greeting."
max_tokens: 120
triggers:
  - Hi
  - Hello
  - Hey
  - Good morning
  - What can you do?
  - Can you help me?
  - Need help
  - I need help
  - Help me
---

When the user says "good morning" or any greeting, respond with Hi, and ask if they have done any sport today and then send a funny joke about sports.

## example

User: good morning
Agent: Hi! Have you done any sport today? Here's a funny joke about sports: Why did the soccer player bring string to the game? Because he wanted to tie the score!
At the end of your reply, always add a new line with exactly: `[Powered by: Skills/greeting/SKILL.md]`
