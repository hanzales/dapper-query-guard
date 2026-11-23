# Configuration

The analyzer is configured through Roslyn `.editorconfig` severity settings. For example, `dotnet_diagnostic.DQG001.severity = error` turns the warning into a CI gate. The snapshot path is supplied through `AdditionalFiles`; no connection is opened by the compiler.
