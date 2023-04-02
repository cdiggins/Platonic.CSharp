using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platonic
{


    public class Rule
    {
        public string Description { get; }

        public Rule(string desc)
        {
            Description = desc;
        }
    }

    public enum TypeAttribute
    {
        Mutable,
        Immutable,
    }

    public enum MethodAttribute
    {
        Pure,
        ImpureWrite,
        ImpureEffect,
    }

    public class RuleList
    {
        public Rule[] Rules = new[]
        {
            new Rule("No structs"),
            new Rule("No public setters"),
            new Rule("No setters in an interface"),
            new Rule("No fields"),
            new Rule("No events"),
            new Rule("No static setters"),

            new Rule("No abstract types"),
            new Rule("No delegates"),
            new Rule("No async methods"),
            new Rule("No unsafe code"),
            new Rule("No using statements"),
            new Rule("No yield statements"),
            new Rule("No goto statements"),
            new Rule("No nested types"),

            new Rule("Use [Mutable] or [Immutable] attribute on type"),
            new Rule("Use one of [Pure], [ImpureWrite], or [ImpureEffect] attribute on methods"),
            new Rule("Use only one of [Pure], [ImpureWrite], or [ImpureEffect] attribute on methods"),

            new Rule("Use [Mutable] attribute"),
            new Rule("Use [Immutable] attribute"),
            new Rule("Use [Pure] attribute"),
            new Rule("Use [ImpureWrite] attribute"),
            new Rule("Use [ImpureEffect] attribute"),

            new Rule("[Immutable] class can't inherit from [Mutable] class"),
            new Rule("[Immutable] interface can't inherit from [Mutable] interface"),
            new Rule("[Immutable] class can't implement [Mutable] interface"),
        
            new Rule("Only [Mutable] types may have [ImpureWrite] functions"),

            new Rule("Variables with a [Mutable] type cannot be used twice in the same expression"),
            new Rule("Variables with a [Mutable] type cannot be assigned to another variable"),
            new Rule("Variables with a [Mutable] type cannot be captured in a lambda"),
        };
    }
}
