// Copyright (c) Artisense. All rights reserved.

using System.Diagnostics.CodeAnalysis;

// StyleCop suppressions for MVP - these would be addressed in production
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "MVP scope - will be addressed in production")]
[assembly: SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1028:Code should not contain trailing whitespace", Justification = "MVP scope - will be addressed in production")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1413:Use trailing comma in multi-line initializers", Justification = "MVP scope - will be addressed in production")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1512:Single-line comments should not be followed by blank line", Justification = "MVP scope - will be addressed in production")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1124:Do not use regions", Justification = "MVP scope - P/Invoke regions are acceptable")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "MVP scope - will be addressed in production")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "MVP scope - will be addressed in production")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1203:Constants should appear before fields", Justification = "MVP scope - will be addressed in production")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "MVP scope - will be addressed in production")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "MVP scope - Windows API constants")]

// SonarLint suppressions for MVP
[assembly: SuppressMessage("SonarAnalyzer.CSharp", "S1144:Remove the unused private field", Justification = "MVP scope - placeholder fields for future implementation")]
[assembly: SuppressMessage("SonarAnalyzer.CSharp", "S2344:Rename this enumeration to remove the 'Flags' suffix", Justification = "MVP scope - Windows API naming")]
[assembly: SuppressMessage("SonarAnalyzer.CSharp", "S3260:Private classes which are not derived in the current assembly should be marked as 'sealed'", Justification = "MVP scope - will be addressed in production")]
[assembly: SuppressMessage("SonarAnalyzer.CSharp", "S3267:Loops should be simplified using the 'Where' LINQ method", Justification = "MVP scope - performance-sensitive code")]
[assembly: SuppressMessage("SonarAnalyzer.CSharp", "S2589:Change this condition so that it does not always evaluate to 'True'", Justification = "MVP scope - defensive programming")]
