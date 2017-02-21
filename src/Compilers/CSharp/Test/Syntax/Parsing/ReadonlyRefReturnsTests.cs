// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.ReadonlyReferences)]
    public class ReadonlyRefReturnsTests : ParsingTests
    {
        public ReadonlyRefReturnsTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact]
        public void ReadonlyRefReturn_CSharp7()
        {
            var text = @"
unsafe class Program
{
    delegate readonly ref int D1();

    static readonly ref T M<T>()
    {
        return ref (new T[1])[0];
    }

    public virtual readonly ref int* P1 => throw null;

    public readonly ref int[][] this[int i] => throw null;
}
";

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7), options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics(
                // (4,14): error CS8107: Feature 'readonly references' is not available in C# 7.  Please use language version 71 or greater.
                //     delegate readonly ref int D1();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "readonly").WithArguments("readonly references", "71").WithLocation(4, 14),
                // (6,12): error CS8107: Feature 'readonly references' is not available in C# 7.  Please use language version 71 or greater.
                //     static readonly ref T M<T>()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "readonly").WithArguments("readonly references", "71").WithLocation(6, 12),
                // (11,20): error CS8107: Feature 'readonly references' is not available in C# 7.  Please use language version 71 or greater.
                //     public virtual readonly ref int* P1 => throw null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "readonly").WithArguments("readonly references", "71").WithLocation(11, 20),
                // (13,12): error CS8107: Feature 'readonly references' is not available in C# 7.  Please use language version 71 or greater.
                //     public readonly ref int[][] this[int i] => throw null;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "readonly").WithArguments("readonly references", "71").WithLocation(13, 12)

                );
        }

        [Fact]
        public void ReadonlyRefReturn_Unexpected()
        {
            var text = @"

class Program
{
    static void Main()
    {
    }

    readonly ref int Field;

    public static readonly ref Program  operator  +(Program x, Program y)
    {
        throw null;
    }

    // this parses fine
    static async readonly ref Task M<T>()
    {
        throw null;
    }

    public readonly ref virtual int* P1 => throw null;

}
";

            ParseAndValidate(text, TestOptions.Regular,
                // (9,27): error CS1003: Syntax error, '(' expected
                //     readonly ref int Field;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("(", ";").WithLocation(9, 27),
                // (9,27): error CS1026: ) expected
                //     readonly ref int Field;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(9, 27),
                // (11,41): error CS1519: Invalid token 'operator' in class, struct, or interface member declaration
                //     public static readonly ref Program  operator  +(Program x, Program y)
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(11, 41),
                // (11,41): error CS1519: Invalid token 'operator' in class, struct, or interface member declaration
                //     public static readonly ref Program  operator  +(Program x, Program y)
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "operator").WithArguments("operator").WithLocation(11, 41),
                // (12,5): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                //     {
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(12, 5),
                // (12,5): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                //     {
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(12, 5),
                // (22,25): error CS1031: Type expected
                //     public readonly ref virtual int* P1 => throw null;
                Diagnostic(ErrorCode.ERR_TypeExpected, "virtual").WithLocation(22, 25),
                // (24,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(24, 1)
                );
        }

        [Fact]
        public void ReadonlyRefReturn_UnexpectedBindTime()
        {
            var text = @"

class Program
{
    static void Main()
    {
        readonly ref int local = ref (new int[1])[0];

        (readonly ref int, readonly ref int Alice)? t = null;

        System.Collections.Generic.List<readonly ref int> x = null;

        Use(local);
        Use(t);
        Use(x);
    }

    static void Use<T>(T dummy)
    {
    }
}
";
            //PROTOTYPE(readonlyRef): binding now falls back on regular "Ref", otherwise there should be one more error on the local declaration

            var comp = CreateCompilationWithMscorlib45(text, new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            comp.VerifyDiagnostics(
                // (9,19): error CS1073: Unexpected token 'ref'
                //         (readonly ref int, readonly ref int Alice)? t = null;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(9, 19),
                // (9,37): error CS1073: Unexpected token 'ref'
                //         (readonly ref int, readonly ref int Alice)? t = null;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(9, 37),
                // (11,50): error CS1073: Unexpected token 'ref'
                //         System.Collections.Generic.List<readonly ref int> x = null;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(11, 50)
            );
        }

    }
}
