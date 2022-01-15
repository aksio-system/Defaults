namespace Aksio.CodeAnalysis.ElementsMustAppearInTheCorrectOrder
{
    /// <summary>
    /// Represents an abstract base <see cref="DiagnosticAnalyzer"/> for requiring a specific order of an element.
    /// </summary>
    public abstract class ElementsMustAppearInTheCorrectOrderAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <summary>
        /// Gets the <see cref="DiagnosticDescriptor">rule</see> for the analyzer.
        /// </summary>
        public abstract DiagnosticDescriptor Rule { get; }

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(ValidateRule, ImmutableArray.Create(SyntaxKind.ClassDeclaration));
        }

        /// <summary>
        /// Gets the element kind we are checking the ordering for.
        /// </summary>
        protected abstract SyntaxKind KindToCheckFor { get; }

        /// <summary>
        /// Gets any modifiers used for filtering for the analyzer to apply its rules for.
        /// </summary>
        protected virtual IEnumerable<SyntaxKind> Modifiers => Array.Empty<SyntaxKind>();

        void ValidateRule(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDeclaration)
            {
                return;
            }

            var elementToCheckFor = KindToElementOrder(KindToCheckFor);

            var members = classDeclaration.Members
                .Select((m, pos) => (Member: m, Position: pos))
                .Where(_ => _.Member.Kind() == KindToCheckFor);

            members = FilterMembersForSupportedModifiers(members);

            var currentKindList = members.ToList();

            // Each instance of the element before the end of the types that should be before it generates an error
            var lastBeforeElementPosition = LastBeforeElementPosition(classDeclaration, elementToCheckFor);
            foreach (var (member, _) in currentKindList.Where(_ => _.Position < lastBeforeElementPosition))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, member.GetLocation()));
            }

            // Each instance of the element after the start of the types that should be after it generates an error
            var firstAfterElementPosition = FirstAfterElementPosition(classDeclaration, elementToCheckFor);
            foreach (var (member, _) in currentKindList.Where(_ => _.Position > firstAfterElementPosition))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, member.GetLocation()));
            }
        }

        IEnumerable<(MemberDeclarationSyntax Member, int Position)> FilterMembersForSupportedModifiers(IEnumerable<(MemberDeclarationSyntax Member, int Position)> members)
        {
            if (Modifiers.Any())
            {
                members = members.Where(_ =>
                {
                    var contains = false;
                    foreach (var modifier in Modifiers)
                    {
                        contains |= _.Member.Modifiers.Any(modifier);
                    }

                    return contains;
                });
            }

            return members;
        }

        int LastBeforeElementPosition(ClassDeclarationSyntax classDeclaration, ElementOrderList elementOrderToCheckFor)
        {
            var otherElementTypes = classDeclaration.Members
                .Select((m, pos) => (ElementOrder: KindToElementOrder(m.Kind()), Position: pos))
                .Where(_ => _.ElementOrder != elementOrderToCheckFor)
                .ToList();

            var elementsThatShouldBeBefore = otherElementTypes.Where(_ => _.ElementOrder < elementOrderToCheckFor).ToList();
            if (elementsThatShouldBeBefore.Count == 0)
            {
                return 0;
            }

            return elementsThatShouldBeBefore.Max(_ => _.Position);
        }

        int FirstAfterElementPosition(ClassDeclarationSyntax classDeclaration, ElementOrderList elementOrderToCheckFor)
        {
            var kindAndPosition = classDeclaration.Members
                .Select((m, pos) => (ElementOrder: KindToElementOrder(m.Kind()), Position: pos))
                .Where(_ => _.ElementOrder != elementOrderToCheckFor)
                .ToList();

            var elementsThatShouldBeAfter = kindAndPosition.Where(_ => _.ElementOrder > elementOrderToCheckFor).ToList();
            if (elementsThatShouldBeAfter.Count == 0)
            {
                return classDeclaration.Members.Count;
            }

            return elementsThatShouldBeAfter.Min(_ => _.Position);
        }

        ElementOrderList KindToElementOrder(SyntaxKind syntaxKind)
        {
            return syntaxKind switch
            {
                SyntaxKind.FieldDeclaration => ElementOrderList.FieldsAndProperties,
                SyntaxKind.PropertyDeclaration => ElementOrderList.FieldsAndProperties,
                SyntaxKind.DelegateDeclaration => ElementOrderList.Delegates,
                SyntaxKind.EventFieldDeclaration => ElementOrderList.Events,
                SyntaxKind.EventDeclaration => ElementOrderList.Events,
                SyntaxKind.ConstructorDeclaration => ElementOrderList.Constructors,
                SyntaxKind.DestructorDeclaration => ElementOrderList.Destructors,
                SyntaxKind.IndexerDeclaration => ElementOrderList.Indexers,
                SyntaxKind.MethodDeclaration => ElementOrderList.Methods,
                _ => ElementOrderList.Undefined
            };
        }
    }
}