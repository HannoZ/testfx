﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Framework;
using Microsoft.Testing.TestInfrastructure;

using VerifyCS = MSTest.Analyzers.Test.CSharpCodeFixVerifier<
    MSTest.Analyzers.TestClassShouldBeValidAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace MSTest.Analyzers.Test;

[TestGroup]
public sealed class TestClassShouldBeValidAnalyzerTests(ITestExecutionContext testExecutionContext) : TestBase(testExecutionContext)
{
    public async Task WhenClassIsPublicAndTestClass_NoDiagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class MyTestClass
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    public async Task WhenClassIsInternalAndTestClass_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            internal class {|#0:MyTestClass|}
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.PublicRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    [Arguments("private")]
    [Arguments("internal")]
    public async Task WhenClassIsInnerAndNotPublicTestClass_Diagnostic(string accessibility)
    {
        var code = $$"""
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            public class OuterClass
            {
                [TestClass]
                {{accessibility}} class {|#0:MyTestClass|}
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.PublicRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    public async Task WhenClassIsInternalAndNotTestClass_NoDiagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            internal class MyTestClass
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    public async Task WhenClassIsPublicAndTestClassAsInnerOfInternalClass_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            internal class OuterClass
            {
                [TestClass]
                public class {|#0:MyTestClass|}
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.PublicRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    public async Task WhenClassIsGeneric_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public class {|#0:MyTestClass|}<T>
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.NotGenericRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    public async Task WhenClassIsNotGenericButAsOuterGeneric_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            public class MyClass<T>
            {
                [TestClass]
                public class {|#0:MyTestClass|}
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.NotGenericRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    public async Task WhenDiscoverInternalsAndTypeIsInternal_NoDiagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [assembly: DiscoverInternals]

            [TestClass]
            internal class MyTestClass
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    public async Task WhenDiscoverInternalsAndTypeIsPrivate_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [assembly: DiscoverInternals]

            public class A
            {
                [TestClass]
                private class {|#0:MyTestClass|}
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.PublicOrInternalRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    public async Task WhenClassIsStaticAndEmpty_NoDiagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public static class MyTestClass
            {
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    public async Task WhenClassIsStaticAndContainsAssemblyInitialize_NoDiagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public static class MyTestClass
            {
                [AssemblyInitialize]
                public static void AssemblyInit(TestContext context)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    public async Task WhenClassIsStaticAndContainsAssemblyCleanup_NoDiagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public static class MyTestClass
            {
                [AssemblyCleanup]
                public static void AssemblyCleanup()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(code);
    }

    public async Task WhenClassIsStaticAndContainsClassInitialize_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public static class {|#0:MyTestClass|}
            {
                [ClassInitialize]
                public static void ClassInit(TestContext testContext)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.NotStaticRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    public async Task WhenClassIsStaticAndContainsClassCleanup_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public static class {|#0:MyTestClass|}
            {
                [ClassCleanup]
                public static void ClassCleanup()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.NotStaticRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    public async Task WhenClassIsStaticAndContainsTestInitialize_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public static class {|#0:MyTestClass|}
            {
                [TestInitialize]
                public static void TestInit()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.NotStaticRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    public async Task WhenClassIsStaticAndContainsTestCleanup_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public static class {|#0:MyTestClass|}
            {
                [TestCleanup]
                public static void TestCleanup()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.NotStaticRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }

    public async Task WhenClassIsStaticAndContainsTestMethod_Diagnostic()
    {
        var code = """
            using Microsoft.VisualStudio.TestTools.UnitTesting;

            [TestClass]
            public static class {|#0:MyTestClass|}
            {
                [TestMethod]
                public static void TestMethod()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            code,
            VerifyCS.Diagnostic(TestClassShouldBeValidAnalyzer.NotStaticRule)
                .WithLocation(0)
                .WithArguments("MyTestClass"));
    }
}
