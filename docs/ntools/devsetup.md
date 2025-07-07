

# Development Environment Setup for ntools

This guide explains how to set up your development environment for the `ntools` project. It automates the installation of `ntools` and all required development tools, ensuring a consistent and reliable onboarding experience for all contributors.

---

## Prerequisites

- **Windows OS** (PowerShell required)
- **PowerShell 5.1+** (or PowerShell Core)
- **Administrator rights** (required for installing system tools)
- **Internet access** (to download modules and tools)

> **Tip:** If you encounter issues, see the Troubleshooting and FAQ sections below.

---

## How to Run the Setup Script

1. **Open PowerShell as Administrator**
   - Right-click on PowerShell and select **Run as administrator**.
2. **Navigate to the project root directory:**
   ```powershell
   cd path\to\ntools
   ```
3. **Run the setup script:**
   ```powershell
   ./dev-setup/install.ps1
   ```

---

## Troubleshooting

- **Admin Rights:** Make sure to run this script as an administrator. If you see an error about permissions, close PowerShell and reopen it as admin.
- **Network Issues:** Ensure you have a stable internet connection. Proxy/firewall settings may block downloads.
- **Script Errors:** Review the output for error messages. Check that all prerequisites are met and files exist in the expected locations.
- **Module Import Fails:** If `install.psm1` fails to import, check that it was downloaded successfully and is not blocked by Windows security settings.

---

## FAQ

**Q: Can I add more tools to the setup?**
A: Yes! Edit `apps.json` to include additional tools. See [nbuildtasks.md](./nbuildtasks.md) for automation details.

**Q: Where do I report issues or request features?**
A: See [backlog.md](../backlog/backlog.md) for instructions on submitting issues and PBIs.

**Q: How do I contribute to ntools?**
A: See the [Contribution Guide](../CONTRIBUTING.md) (if available) or contact the maintainers.

**Q: Where can I find more documentation?**
A: See the [ntools documentation index](./ntools.md) for tool usage, automation, and troubleshooting guides.

---

## Related Documentation

- [ntools documentation index](./ntools.md)
- [nbuildtasks.md](./nbuildtasks.md)
- [backlog.md](../backlog/backlog.md)
- [version-automation-guide.md](../version-automation-guide.md)

---

> **Last updated:** July 4, 2025

