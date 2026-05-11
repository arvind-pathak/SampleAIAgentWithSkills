# SampleAIAgentWithSkills
This project use skill.md file with different intents to process user natural language queries to LLM 
# Employee Support AI Agent

## Overview

This project is a simple AI Agent application built to understand how AI Agents use Skills (Tools / Functions) to answer user questions dynamically.

The project demonstrates:

* AI Agent concepts
* Skill-based architecture
* Semantic Kernel function calling
* Local LLM integration using Ollama + Phi3
* Skill discovery and invocation
* Natural language interaction

The goal is educational and focused on understanding the core concept of Agentic AI.

---

# Tech Stack

| Component     | Technology                |
| ------------- | ------------------------- |
| UI            | Angular                   |
| Backend Agent | .NET 8 Web API            |
| AI Framework  | Microsoft Semantic Kernel |
| LLM           | Ollama Phi3               |
| Data Source   | Mock JSON                 |
| Communication | REST API                  |

---

# Main Learning Objective

Understand how an AI Agent:

1. Receives user input
2. Understands intent using an LLM
3. Selects the correct Skill dynamically
4. Executes the Skill
5. Generates a natural language response

This project intentionally avoids:

* databases
* vector search
* MCP
* authentication
* multi-agent systems
* complex orchestration

---

# Example User Questions

## Greeting

User:

```text
Hi
```

Agent:

```text
Hello! How can I help you today?
```

Uses:

* GreetingSkill

---

## Leave Balance

User:

```text
How many leaves does John have?
```

Agent:

```text
John has 12 leave days remaining.
```

Uses:

* LeaveSkill

---

## Employee Information

User:

```text
Tell me about Sarah
```

Agent:

```text
Sarah works in the Finance department.
```

Uses:

* EmployeeSkill

---

# High Level Architecture

```text
Angular UI
    ↓
.NET 8 AI Agent API
    ↓
Semantic Kernel
    ↓
Ollama Phi3
    ↓
Registered Skills
    ↓
Mock JSON Data
```

---

# Project Structure

```text
employee-support-agent/
│
├── frontend-angular/
│
├── backend-agent/
│   │
│   ├── Skills/
│   │   ├── LeaveSkill.cs
│   │   ├── EmployeeSkill.cs
│   │   └── GreetingSkill.cs
│   │
│   ├── SkillDefinitions/
│   │   ├── LeaveSkill.md
│   │   ├── EmployeeSkill.md
│   │   └── GreetingSkill.md
│   │
│   ├── Data/
│   │   └── employees.json
│   │
│   ├── Services/
│   │   └── AgentService.cs
│   │
│   ├── Controllers/
│   │   └── ChatController.cs
│   │
│   └── Program.cs
│
└── README.md
```

---

# Functional Requirements

## Angular Frontend

Create a minimal Angular application with:

* Simple chat interface
* Input textbox
* Send button
* Chat message history
* Call backend API using HttpClient
* Display AI responses

---

# Backend Requirements

Create an ASP.NET Core Web API project using .NET 8.

The backend should:

* Integrate Semantic Kernel
* Connect to Ollama Phi3
* Register Skills dynamically
* Accept user chat messages
* Allow Semantic Kernel to automatically invoke skills
* Return natural language responses

---

# Ollama Setup

Install Ollama locally.

Run Phi3 model:

```bash
ollama run phi3
```

Ollama endpoint:

```text
http://localhost:11434
```

---

# Semantic Kernel Requirements

Use:

* Microsoft.SemanticKernel
* Ollama chat completion connector

Enable:

* Automatic function calling
* Tool invocation

The agent must allow the LLM to decide which skill to use.

Do NOT hardcode if-else conditions for intent matching.

---

# Skills

## LeaveSkill

Responsibilities:

* Return employee leave balance

Functions:

```csharp
GetLeaveBalance(string employeeName)
```

---

## EmployeeSkill

Responsibilities:

* Return employee department information

Functions:

```csharp
GetEmployeeInfo(string employeeName)
```

---

## GreetingSkill

Responsibilities:

* Handle greetings

Functions:

```csharp
SayHello()
```

---

# Skill Definition Files

Each skill must have a corresponding markdown definition file.

Example:

## LeaveSkill.md

```markdown
# LeaveSkill

## Purpose
Provides leave balance information for employees.

## Available Functions

### GetLeaveBalance
Returns remaining leave balance for employee.

## Example Questions
- How many leaves does John have?
- Show leave balance for Sarah
```

These files are used to help explain skills and document agent capabilities.

---

# Mock Data

Use a JSON file:

## employees.json

```json
{
  "John": {
    "department": "IT",
    "leaveBalance": 12
  },
  "Sarah": {
    "department": "Finance",
    "leaveBalance": 8
  }
}
```

---

# API Design

## POST /api/chat

Request:

```json
{
  "message": "How many leaves does John have?"
}
```

Response:

```json
{
  "response": "John has 12 leave days remaining."
}
```

---

# Important Design Rules

## MUST

* Use Semantic Kernel automatic function calling
* Let LLM decide which skill to invoke
* Use clean layered architecture
* Keep implementation simple and educational
* Add comments explaining agent flow

---

## MUST NOT

* No hardcoded intent matching
* No if-else routing for skills
* No database
* No authentication
* No MCP
* No vector database

---

# Primary Educational Goal

The most important concept to demonstrate:

```text
AI Agent dynamically selects Skills using semantic reasoning.
```

The agent should behave like:

```text
Question about leave
→ Use LeaveSkill

Question about employee info
→ Use EmployeeSkill

Greeting
→ Use GreetingSkill
```

without manual routing logic.

---

# Step-by-Step Test Flow

## Prerequisites — Three Services Must Be Running

| Service | URL | Terminal Command |
| ------- | --- | ---------------- |
| Ollama (Phi3) | `http://localhost:11434` | `ollama serve` |
| .NET Backend Agent | `http://localhost:5222` | `cd backend-agent && dotnet run --launch-profile http` |
| Angular Frontend | `http://localhost:4200` | `cd frontend-angular && ng serve --open` |

Open three separate terminals and start each service in order.

---

## Step 1 — Test GreetingSkill

Type in the chat box:

```text
Hi
```

Expected response:

```text
Hello! I am your Employee Support Agent. I can help you with:
- Employee leave balances
- Employee department info
```

**What happens internally:**

```text
Angular POSTs { "message": "Hi" } to POST /api/chat
  ↓
Semantic Kernel sends message + all 3 skill descriptions to Phi3
  ↓
Phi3 reads [Description("Responds to greetings, hello, hi...")] on SayHello()
  ↓
Phi3 picks GreetingSkill — no code condition involved
  ↓
SK invokes GreetingSkill.SayHello()
  ↓
Result returned to Phi3 → Phi3 writes final reply
  ↓
Angular displays response
```

**Skill invoked:** `GreetingSkill.SayHello()`

---

## Step 2 — Test LeaveSkill

Type in the chat box:

```text
How many leaves does John have?
```

Expected response:

```text
John has 12 leave days remaining.
```

**What happens internally:**

```text
Phi3 reads [Description("Returns the remaining leave balance...")] on GetLeaveBalance()
  ↓
Phi3 picks LeaveSkill — extracts employeeName = "John" from the message
  ↓
SK calls LeaveSkill.GetLeaveBalance("John")
  ↓
Looks up employees.json → John.leaveBalance = 12
  ↓
Returns "John has 12 leave days remaining."
  ↓
Phi3 wraps it into a natural language reply
```

**Skill invoked:** `LeaveSkill.GetLeaveBalance("John")`

---

## Step 3 — Test EmployeeSkill

Type in the chat box:

```text
Tell me about Sarah
```

Expected response:

```text
Sarah works in the Finance department and has 8 leave days remaining.
```

**What happens internally:**

```text
Phi3 reads [Description("Returns department and profile information...")] on GetEmployeeInfo()
  ↓
Phi3 picks EmployeeSkill — extracts employeeName = "Sarah"
  ↓
SK calls EmployeeSkill.GetEmployeeInfo("Sarah")
  ↓
Looks up employees.json → Sarah.department = "Finance"
  ↓
Returns department and leave info
  ↓
Phi3 formulates the final reply
```

**Skill invoked:** `EmployeeSkill.GetEmployeeInfo("Sarah")`

---

## Step 4 — Test Skill Selection with Varied Phrasing

The key demo point: the LLM selects the right skill regardless of how the question is phrased.
No hardcoded routing. No if-else conditions. Pure semantic reasoning.

| Question | Skill Selected |
| -------- | -------------- |
| `What's Emily's remaining time off?` | LeaveSkill |
| `Which team does Mike belong to?` | EmployeeSkill |
| `Show leave balance for Sarah` | LeaveSkill |
| `Give me info about John` | EmployeeSkill |
| `Hey there` | GreetingSkill |
| `What department is Mike in?` | EmployeeSkill |
| `Does Emily have any annual leave left?` | LeaveSkill |

---

## Step 5 — Use the Quick Demo Buttons

The Angular UI has one-click buttons at the bottom of the screen for live demos:

| Button | Sends | Skill |
| ------ | ----- | ----- |
| 👋 Hi | `Hi` | GreetingSkill |
| 🏖️ John's leave | `How many leaves does John have?` | LeaveSkill |
| 👩 About Sarah | `Tell me about Sarah` | EmployeeSkill |
| 🏢 Mike's dept | `What department is Mike in?` | EmployeeSkill |
| 📅 Emily's leave | `Show leave balance for Emily` | LeaveSkill |

---

## Troubleshooting

| Symptom | Fix |
| ------- | --- |
| Angular shows "Error: Could not connect to backend" | Backend is not running — start it with `dotnet run --launch-profile http` inside `backend-agent/` |
| Backend starts but agent hangs forever | Ollama is not serving — run `ollama serve` in a separate terminal |
| Agent replies with generic text and no skill is used | Phi3 is a small model — rephrase more explicitly e.g. "What is John's leave balance?" |
| Port 5222 already in use | Change `applicationUrl` in `Properties/launchSettings.json` and update `apiUrl` in `frontend-angular/src/app/app.ts` |
| Ollama error: address already in use | Ollama is already running in the background — this is fine, skip `ollama serve` |

---

# Future Enhancements (NOT NOW)

Possible future additions:

* MCP Server
* Vector Database
* Memory
* Multi-agent system
* Adaptive Cards
* Azure OpenAI
* Teams integration
* Approval workflows

```
```
