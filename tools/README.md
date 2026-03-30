# Tools Configuration

This directory defines external tool integrations and custom tool definitions for the agent team.

## Available Tool Categories

### File Management
- Read/write project artifacts in `outputagent/`
- Manage context files for each agent

### Search & Retrieval
- Search across decision records
- Find related requirements or risks
- Look up previous architecture decisions

### Generation
- Create artifacts from skill templates
- Generate diagrams (Mermaid format)
- Produce status reports from project data

## Adding New Tools

Create a `<tool-name>.md` file describing:
1. **Purpose** — What the tool does
2. **Inputs** — What information it needs
3. **Outputs** — What it produces
4. **Usage** — When and how to invoke it
