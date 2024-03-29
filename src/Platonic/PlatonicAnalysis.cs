﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Ptarmigan.Utils.Roslyn;
using Compilation = Ptarmigan.Utils.Roslyn.Compilation;

namespace Platonic;

public class PlatonicFileAnalysis
{
    public List<PlatoTypeAnalysis> Types { get; } = new();

    public SyntaxTree Tree { get; }
    public SemanticModel Model { get; }
    public string FileName => Tree.FilePath;

    public PlatonicFileAnalysis(SyntaxTree tree, SemanticModel model)
    {
        Tree = tree;
        Model = model;
        Types = TypeDeclarations.Select(td => new PlatoTypeAnalysis(model, td)).ToList();
    }

    public IEnumerable<UsingDirectiveSyntax> UsingDirectives
        => Tree.GetRoot().ChildNodes().OfType<UsingDirectiveSyntax>();

    public IEnumerable<EnumDeclarationSyntax> EnumDeclarations
        => Tree.GetRoot().DescendantNodesAndSelf().OfType<EnumDeclarationSyntax>();

    public IEnumerable<TypeDeclarationSyntax> TypeDeclarations
        => Tree.GetRoot().DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>();
}

public class PlatonicProjectAnalysis
{
    public Dictionary<INamedTypeSymbol, PlatoTypeAnalysis> SymbolLookup =
        new Dictionary<INamedTypeSymbol, PlatoTypeAnalysis>(SymbolEqualityComparer.Default);

    public List<PlatonicFileAnalysis> Files { get; } = new(); 
    public Compilation Compilation { get; }

    public PlatonicProjectAnalysis(Compilation compilation)
    {
        Compilation = compilation;
        foreach (var st in compilation.SyntaxTrees)
        {
            Files.Add(new PlatonicFileAnalysis(st, compilation.Compiler.GetSemanticModel(st)));
        }

        foreach (var f in Files)
        foreach (var t in f.Types)
            SymbolLookup.Add(t.TypeSymbol, t);
    }
}

public abstract class PlatonicAnalysisContext
{
    /// <summary>
    /// The symbol
    /// </summary>
    public ISymbol Symbol;

    /// <summary>
    /// The associated semantic model
    /// </summary>
    public SemanticModel Model;

    /// <summary>
    /// The associated operation
    /// </summary>
    public IOperation? Operation;

    /// <summary>
    /// Node requested
    /// </summary>
    public SyntaxNode Node;

    /// <summary>
    /// Members have either an expression or statement body. 
    /// </summary>
    public SyntaxNode? StatementOrExpression;

    /// <summary>
    /// Data flow analysis 
    /// </summary>
    public DataFlowAnalysis? DataFlow;

    /// <summary>
    /// Control flow analysis. 
    /// </summary>
    public ControlFlowAnalysis? ControlFlow;

    /// <summary>
    /// Get the assignment operations 
    /// </summary>
    public IEnumerable<IAssignmentOperation> Assignments
        => Operation?.DescendantsAndSelf().OfType<IAssignmentOperation>()
           ?? Enumerable.Empty<IAssignmentOperation>();

    /// <summary>
    /// Get the assignment operations 
    /// </summary>
    public IEnumerable<IMemberReferenceOperation> MemberReferences
        => Operation?.DescendantsAndSelf().OfType<IMemberReferenceOperation>()
           ?? Enumerable.Empty<IMemberReferenceOperation>();

    public abstract string Name { get; }

    public PlatonicAnalysisContext(SemanticModel model, ISymbol symbol, SyntaxNode node)
    {
        if (symbol == null) throw new ArgumentNullException(nameof(symbol));
        Model = model;
        Symbol = symbol;
        StatementOrExpression = node.GetAssociatedStatementOrExpression();
        Node = node;
        Operation = model.GetOperation(StatementOrExpression);
        DataFlow = model.GetDataFlowAnalysis(StatementOrExpression);
        if (StatementOrExpression is StatementSyntax st)
            ControlFlow = model.AnalyzeControlFlow(st);
    }
}

public class PlatoTypeAnalysis : PlatonicAnalysisContext
{
    public PlatoTypeAnalysis(SemanticModel model, TypeDeclarationSyntax node)
        : base(model, ModelExtensions.GetDeclaredSymbol(model, node), node) 
    {
        foreach (var m in TypeSyntax.Members)
        {
            if (m is BaseMethodDeclarationSyntax mds)
            {
                Methods.Add(new PlatonicMethodAnalysis(this, mds));
            }
            else if (m is FieldDeclarationSyntax fds)
            {
                foreach (var v in fds.Declaration.Variables)
                {
                    Fields.Add(new PlatonicFieldAnalysis(this, fds, v));
                }
            }
            else if (m is PropertyDeclarationSyntax pds)
            {
                Properties.Add(new PlatonicPropertyAnalysis(this, pds));
            }
            else if (m is IndexerDeclarationSyntax ids)
            {
                Indexers.Add(new PlatonicIndexerAnalysis(this, ids));
            }
            else if (m is TypeDeclarationSyntax tds)
            {
                throw new Exception("Nested types not supported");
            }
            else
            {
                // TODO: handle nested TypeDeclaration
                throw new Exception($"Unhandled member {m}");
            }
        }
    }

    public override string Name
        => TypeSymbol?.Name ?? "_unknowntype_";

    public TypeDeclarationSyntax TypeSyntax
        => (TypeDeclarationSyntax)Node;

    public INamedTypeSymbol? TypeSymbol 
        => Symbol as INamedTypeSymbol;

    public readonly List<PlatonicMethodAnalysis> Methods = new();
    public readonly List<PlatonicFieldAnalysis> Fields = new();
    public readonly List<PlatonicPropertyAnalysis> Properties = new();
    public readonly List<PlatonicIndexerAnalysis> Indexers = new();

    public IEnumerable<PlatoMemberAnalysis> Members 
        => Methods.Cast<PlatoMemberAnalysis>().Concat(Fields).Concat(Properties).Concat(Indexers);

    public string GetTypeKind()
    {
        switch (TypeSymbol.TypeKind)
        {
            case TypeKind.Class: return "class";
            case TypeKind.Struct: return "struct";
            case TypeKind.Enum: return "enum";
            case TypeKind.Interface: return "interface";
        }

        return "_unknownkind_";
    }

    public bool HasWritableFields => Fields.Any(f => !f.IsReadOnly && !f.IsConst);
    public bool HasFields => Fields.Any();
    public IEnumerable<PlatonicMethodAnalysis> Setters => Properties.Select(p => p.Setter).Where(x => x != null);
    public bool HasAnyPublicSetters => Setters.Any(p => p.IsPublic);
    public bool HasAnySetters => Setters.Any();
}

public abstract class PlatoMemberAnalysis : PlatonicAnalysisContext
{
    public PlatoTypeAnalysis ParentType { get; }

    public PlatoMemberAnalysis(PlatoTypeAnalysis parentType, ISymbol? symbol, MemberDeclarationSyntax? node)
        : base(parentType.Model, symbol, node)
    {
        ParentType = parentType;
    }

    public MemberDeclarationSyntax MemberSyntax 
        => (MemberDeclarationSyntax)Node;
    
    public bool IsPublic 
        => MemberSyntax.IsPublicMember();
    
    public bool IsStatic 
        => MemberSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));

    public abstract ITypeSymbol MemberType { get; }
}

public class PlatonicMethodAnalysis : PlatoMemberAnalysis
{
    public PlatonicMethodAnalysis(PlatoTypeAnalysis parentType, BaseMethodDeclarationSyntax node)
        : base(parentType, ModelExtensions.GetDeclaredSymbol(parentType.Model, node), node)
    {
    }

    public BaseMethodDeclarationSyntax MethodSyntax 
        => (BaseMethodDeclarationSyntax)Node;

    public IMethodSymbol MethodSymbol
        => (IMethodSymbol)Symbol;

    public override string Name 
        => MethodSymbol?.Name ?? "";
    
    /// <summary>
    /// Does it have any side-effects? 
    /// </summary>
    public bool IsPure;

    /// <summary>
    /// Noted if there is no reading or writing of instance data. 
    /// </summary>
    public bool ShouldBeStatic;

    /// <summary>
    /// A Platonic method is either pure or impure, and if it is impure
    /// makes no copies of the impure variables.
    /// A static Platonic method must be read-only. 
    /// </summary>
    public bool IsPlatonic;

    /// <summary>
    /// Where is the method used 
    /// </summary>
    public readonly List<SyntaxNode> UseList = new();

    public override ITypeSymbol MemberType
        => MethodSymbol.ReturnType;
}

public class PlatonicFieldAnalysis : PlatoMemberAnalysis
{
    public PlatonicFieldAnalysis(PlatoTypeAnalysis parentType, FieldDeclarationSyntax field, VariableDeclaratorSyntax node)
        : base(parentType, ModelExtensions.GetDeclaredSymbol(parentType.Model, node), field)
    {
    }

    public IFieldSymbol? FieldSymbol
        => Symbol as IFieldSymbol;

    public FieldDeclarationSyntax FieldSyntax
        => (FieldDeclarationSyntax)Node;

    public bool IsReadOnly 
        => FieldSymbol?.IsReadOnly ?? false;

    public bool IsConst 
        => FieldSymbol?.IsConst ?? false;

    public override string Name
        => FieldSymbol?.Name ?? "";

    public override ITypeSymbol MemberType
        => FieldSymbol?.Type;
}

public class PlatonicIndexerAnalysis : PlatoMemberAnalysis
{
    public PlatonicIndexerAnalysis(PlatoTypeAnalysis parentType, IndexerDeclarationSyntax node)
        : base(parentType, ModelExtensions.GetDeclaredSymbol(parentType.Model, node), node)
    {
    }

    public IPropertySymbol? IndexerSymbol
        => Symbol as IPropertySymbol;

    public IndexerDeclarationSyntax IndexerSyntax
        => (IndexerDeclarationSyntax)Node;

    public override string Name => "this";

    public override ITypeSymbol MemberType 
        => IndexerSymbol.Type;
}

public class PlatonicPropertyAnalysis : PlatoMemberAnalysis
{
    public PlatonicPropertyAnalysis(PlatoTypeAnalysis parentType, PropertyDeclarationSyntax node)
        : base(parentType, ModelExtensions.GetDeclaredSymbol(parentType.Model, node), node)
    {
        var getterSyntax = PropertySymbol?.GetMethod?.GetSyntax();
        if (getterSyntax != null)
        {
            Getter = new PlatonicMethodAnalysis(parentType, getterSyntax);
        }

        var setterSyntax = PropertySymbol?.SetMethod?.GetSyntax();
        if (setterSyntax != null)
        {
            Setter = new PlatonicMethodAnalysis(parentType, getterSyntax);
        }

        // TODO: look for IPropertyReferenceOperation 
        // TODO: look for memberAccessExpressionSyntax
    }

    public IPropertySymbol? PropertySymbol
        => Symbol as IPropertySymbol;

    public PropertyDeclarationSyntax PropertySyntax
        => (PropertyDeclarationSyntax)Node;

    public IReadOnlyList<AccessorDeclarationSyntax> AccessorNodes 
        => PropertySyntax.AccessorList?.Accessors.ToArray() 
           ?? System.Array.Empty<AccessorDeclarationSyntax>();

    public PlatonicMethodAnalysis? Getter;

    public PlatonicMethodAnalysis? Setter;

    public override string Name
        => PropertySymbol?.Name ?? "";

    public override ITypeSymbol MemberType
        => PropertySymbol?.Type;
}

public static class PlatonicAnalysis
{
    // TODO:
    // Question: are fields set anywhere other than within that class's constructor? 
    // If not, then the field can be made "private". 

    // TODO:
    // Structs should be made into classes.

    // TODO:
    // Handle advanced language features, like records.
    // For now: just don't use it. 
    // In the future, downgrade them. 

    // TODO:
    // Distinguish between "platonic" and "side effect free"

    // TODO: 
    // Create a white-list of types. 

    // TODO:
    // Add a "[Pure]" marker to functions that we know are pure. 

    // TODO: 
    // Maybe I should Have lots of attributes that we can add. 

    // TODO:
    // Add a "[Impure]" marker to functions that we know are impure, but still Platonic.

    // TODO:
    // Should I also have a "[Platonic]" marker? 

    // TODO: 
    // What about "mutable" types? I could add a "[Mutable]" marker to them. 

    // TODO: 
    // Note: maybe I should just write out a list of everything. 

    // TODO:
    // Identify troublesome Lambdas. 

    // TODO:
    // Find all of the types that are used. 

    // TODO: 
    // Make a sl

    // TODO:
    // Write some code-fixers. https://www.meziantou.net/writing-a-language-agnostic-roslyn-analyzer-using-ioperation.htm
    //https://github.com/meziantou/Meziantou.Analyzer/blob/main/src/Meziantou.Analyzer/Rules/MakeMemberReadOnlyAnalyzer.cs#L68-L83

    public static IEnumerable<IFieldSymbol> GetAllFields(this ITypeSymbol symbol)
    {
        // 
        //https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.operationextensions.descendants?view=roslyn-dotnet-4.3.0#microsoft-codeanalysis-operations-operationextensions-descendants(microsoft-codeanalysis-ioperation)
        throw new NotImplementedException();
    }

    public static IEnumerable<ISymbol> VariablesWritten(SemanticModel model, SyntaxNode node)
        => model.AnalyzeDataFlow(node).WrittenInside;


    public static bool IsPropertyPlatonic(this SemanticModel model, SyntaxNode node)
    {
        throw new NotImplementedException();
    }

    // A pure method does not modify the fields of the class and it has no side=effects.


    // public static bool IsTypePure(this SemanticModel model)

    // TODO: 
    // If a method is [Impure] a side-effect (e.g., uses an impure parentType) 
    // It does not mean that the enclosing parentType is impure.  

    /// <summary>
    /// Checks if a method is platonic.
    /// Check that any mutable types aren't copied. 
    /// 1) They aren't assigned to anything (including arrays, and fields, and local variables)
    /// 2) That they aren't captured quietly in a lambda
    /// </summary>
    public static bool IsMethodPlatonic(SemanticModel model, MethodDeclarationSyntax method, HashSet<ITypeSymbol> immutableTypes, List<string> reasons)
    {
        if (method == null)
        {
            reasons.Add("Could not find method");
            return false;
        }

        var sym = ModelExtensions.GetDeclaredSymbol(model, method) as IMethodSymbol;
        if (sym == null)
        {
            reasons.Add("Could not find symbol");
            return false;
        }

        var allParametersAreImmutable = sym.Parameters.Select(p => p.Type).All(t => immutableTypes.Contains(t));

        var op = model.GetOperation(method.Body);

        var assignments = op.DescendantsAndSelf().OfType<IAssignmentOperation>();

        foreach (var ass in assignments)
        {
            if (ass.Target is IFieldReferenceOperation fro)
            {
                // We are assigning to a field. 
                // Is it inside of this class? Or another one? 
                reasons.Add($"Assignment to {fro.Field.Name} occurs");
            }

            var t = ass.Value.Type;

            if (t == null)
            {
                reasons.Add($"Could not determine parentType of assigned value {ass.Value}");
            }
            else
            {
                if (!immutableTypes.Contains(t))
                {
                    reasons.Add($"The parentType {t} of the assigned value {ass.Value} is not immutable: you can't copy mutable types!");
                }
            }
        }

        // Check that lambdas don't capture mutable types
        var lambdas = method.GetLambdas();
        foreach (var lambda in lambdas)
        {
            foreach (var v in model.GetCapturedVariables(lambda))
            {
                var t = model.GetTypeSymbol(v);
                if (!immutableTypes.Contains(t))
                {
                    reasons.Add($"A captured variable {v} has a mutable parentType {t}");
                }
            }
        }

        return reasons.Count == 0;
    }

    public static bool IsImmutable(this ITypeSymbol symbol, List<string> reasons)
    {
        // All fields are readonly 
        foreach (var m in symbol.GetDeclaredAndBaseMembers())
        {
            if (m is IFieldSymbol fs)
            {
                if (!fs.IsReadOnly && !fs.IsConst)
                {
                    var node = fs.GetSyntax();
                    if (node != null)
                        if (node.IsPublicMember())
                            reasons.Add($"ParentType has a public field {fs.Name} that is not readonly or const");
                }
            }
        }

        return reasons.Count == 0;
    }
}