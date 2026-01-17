# Agent Skills

This directory contains skills for AI agents, compatible with [agentskills.io](https://agentskills.io).

## What are Skills?

Skills are folders of instructions, scripts, and resources that agents can discover and use to perform tasks more accurately and efficiently. They use progressive disclosure to manage context efficiently.

## Folder Structure

Each skill is a directory containing:

```
skill-name/
├── SKILL.md       # Required: instructions + metadata
├── scripts/       # Optional: executable code
├── references/    # Optional: documentation
└── assets/        # Optional: templates, resources
```

## Creating a New Skill

1. **Create a directory** with a kebab-case name (lowercase, hyphens only)
   - Valid: `my-skill`, `api-integration`, `data-processor`
   - Invalid: `MySkill`, `my_skill`, `my--skill`

2. **Create SKILL.md** with required YAML frontmatter:

```markdown
---
name: skill-name
description: Clear description of what this skill does and when to use it
---

# Skill Instructions

Detailed instructions, guidelines, and examples for the agent...

## Usage

Step-by-step instructions...

## Examples

...
```

### Required Fields

- **name**: Must match directory name. 1-64 characters, lowercase letters, numbers, and hyphens only. Cannot start/end with hyphens or contain consecutive hyphens.
- **description**: Max 1024 characters. Describes what the skill does and when to use it.

### Optional Fields

- **license**: License identifier or reference to bundled license file
- **compatibility**: Max 500 characters. Environment requirements (products, system packages, network access)
- **metadata**: Key-value pairs for additional properties
- **allowed-tools**: Space-delimited list of pre-approved tools (experimental)

## Best Practices

1. **Keep skills focused**: Each skill should do one thing well
2. **Write clear descriptions**: Help agents understand when to use the skill
3. **Provide examples**: Include input/output examples in the skill body
4. **Optimize context**: Keep SKILL.md under 5000 tokens when possible
5. **Use progressive disclosure**: Put detailed documentation in `references/` directory

## Progressive Disclosure

Skills use progressive disclosure to manage context efficiently:

1. **Discovery**: Agents load only name and description (~100 tokens per skill)
2. **Activation**: When a task matches, agent reads full SKILL.md (<5000 tokens recommended)
3. **Deep dive**: Referenced files or scripts load only when needed

## References

- [Agent Skills Specification](https://agentskills.io/specification)
- [Agent Skills GitHub](https://github.com/agentskills/agentskills)
- [Example Skills](https://github.com/anthropics/skills)
