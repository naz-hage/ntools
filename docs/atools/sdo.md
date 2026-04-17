# sdo - Simple DevOps Operations Tool (Deprecated)

> ⚠️ **DEPRECATED**: The Python implementation of SDO has been deprecated. Please use **sdo.net** (C# version) instead.

## Migration to sdo.net (C# Version)

The Python SDO tool has been replaced with a modern C# implementation called **sdo.net**, which provides:

- **Full Feature Parity**: All Python SDO commands work identically in the C# version
- **2x+ Performance Improvement**: Faster execution for all operations
- **Better Integration**: Native .NET integration with no additional runtime dependencies
- **Same Command Interface**: Commands work exactly the same (e.g., sdo workitem list, sdo repo show, etc.)

## Using sdo.net

For DevOps operations across Azure DevOps and GitHub platforms, use the C# implementation:

\\\ash
sdo workitem list
sdo repo show
sdo pr create --file pr.md
\\\

See the main [ntools documentation](../index.md) for detailed usage of sdo.net (C# version).

## Documentation

For SDO command reference and usage patterns, refer to:
- [SDO Command Mappings](../../Sdo/mapping.md) - Mappings between SDO commands and GitHub/Azure CLI
- [ntools Usage Guide](../usage.md) - Complete tool usage documentation

## Support

If you need help with sdo.net, please refer to the main ntools documentation or create an issue on [GitHub](https://github.com/naz-hage/ntools/issues).
