namespace Aksio.CodeAnalysis.ExceptionShouldBeSpecific
{
    public class UnitTests : CodeFixVerifier
    {
        [Fact]
        public void ThrowingCustomException()
        {
            const string content = @"
                using System;

                namespace MyNamespace
                {
                    public class MyException : Exception
                    {

                    }

                    public class MyClass
                    {
                        public void MyMethod()
                        {
                            throw new MyException();
                        }
                    }
                }       
            ";

            VerifyCSharpDiagnostic(content);
        }

        [Fact]
        public void ThrowingException()
        {
            const string content = @"
                using System;

                namespace MyNamespace
                {
                    public class MyClass
                    {
                        public void MyMethod()
                        {
                            throw new Exception();
                        }
                    }
                }       
            ";

            var expected = new DiagnosticResult
            {
                Id = Analyzer.Rule.Id,
                Message = (string)Analyzer.Rule.MessageFormat,
                Severity = Analyzer.Rule.DefaultSeverity,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 10, 29)
                }
            };

            VerifyCSharpDiagnostic(content, expected);
        }

        [Fact]
        public void ThrowingArgumentException()
        {
            const string content = @"
                using System;

                namespace MyNamespace
                {
                    public class MyClass
                    {
                        public void MyMethod()
                        {
                            throw new ArgumentException();
                        }
                    }
                }       
            ";

            var expected = new DiagnosticResult
            {
                Id = Analyzer.Rule.Id,
                Message = (string)Analyzer.Rule.MessageFormat,
                Severity = Analyzer.Rule.DefaultSeverity,
                Locations = new[]
                {
                    new DiagnosticResultLocation("Test0.cs", 10, 29)
                }
            };

            VerifyCSharpDiagnostic(content, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new Analyzer();
        }
    }
}