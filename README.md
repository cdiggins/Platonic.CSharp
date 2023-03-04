# Platonic.CSharp


**Platonic C#** is a set of style and coding guidelines for cross-platform C#
that encourage the usage of functional programming, referential transparency,
and immutable data structures. 

![platonic_solids_1024x400](https://user-images.githubusercontent.com/1759994/222923632-e2d85788-47e5-402a-8a2b-753fed88030d.jpg)

[Image by Johnson Cameraface](https://www.flickr.com/photos/54459164@N00/4184437649) licensed usage under CC BY-NC-SA 2.0

The purpose of these rules are to:
1. Make it easier for tools to analyze, transform, translate, and optimize C# code
2. Make it simpler and faster to develop high quality software 

Platonic C# code can be converted automatically into a pure functional language called 
[Plato](https://github.com/cdiggins/plato) 
which in turn has an optimizer and can be easily compiled or translated to different targets 
(including JavaScript, and back to C#). 

# Background 

I have been using functional programming in C# for several years now, largely in the domain 
of real-time 3D geometry processing. The idea of Platonic C# has evolved from a set of coding 
guidelines that I follow, and have enforced with development teams that I lead.  

Even though C# has released a rich set of syntactic constructs over recent years for 
writing code in a functional style, performance of functional programming patterns remains very poor
due to how the C# and .NET compilers process and handles lambdas. This is less a short-coming 
of the compiler and more of an issue with the language design, which allows unconstrained 
side-effects and reflection. These provide a barrier to a whole family of interesting rewriting optimizations.  

The other challenge with C# is that there is a split in terms of what language features can be used 
where. Many popular plug-in APIs (including Unity and Visual Studio) require using older versions 
of the language which have fewer features.   

# Uniqueness Typing for Controlled Mutability

The key idea of Platonic C# is to restrict mutable data types so that they have at most one reference at any time.
This idea is known as a [unique type](https://en.wikipedia.org/wiki/Uniqueness_type) which is a restricted form 
of [linear type](https://en.wikipedia.org/wiki/Substructural_type_system#Linear_type_systems).

This allows functions to retain [referential transparency](https://en.wikipedia.org/wiki/Referential_transparency) 
even when using mutable types. 

# The Rules

## Language Rules

1. Only C# 7.3 and .NET Standard 2.0 is supported  
1. No unsafe code is allowed 

## Immutability by Default

1. All data types (classes, structs, and interfaces) are immutable by default 
1. An immutable data type does not allow any property setters or changing of fields from any internal method or external code  
1. Immutable data types cannot have fields or properties that are mutable data types

## Mutability and External Libraries

1. Mutable types must be annotated with a `[Mutable]` attribute. 
1. Only classes can be declared as mutable 
1. Mutable data types cannot be captured in a lambda or a delegate 
1. Classes and interfaces imported from other libraries (including System) are assumed to be mutable data types, except for `System.String`
1. Arrays are considered a mutable data types
1. Structs imported from other libraries are treated as immutable
1. A mutable type can only be used in one place at a time. It cannot be assigned to different variables, fields, or formal arguments.

 
# Implementation Details

* The precise type and origin of the MutableAttribute does not matter, you can define it locally if not imported from another class. Analyzers and other tools will look for any attribute with the name `MutableAttribute`.  
 
# Birth of Plato 

Platonic C# came from the idea of embedding a new language in C# that was pure functional and 
cross-platform. Pure functional languages offer opportunities for tools to rewrite and optimize code 
that cannot be safely applied to a language with side effects. 

The challenge with embedding in C# directly was the requirement for a lot of boilerplate that 
does not make sense in a pure functional language with an advanced compiler.   

Eventually this idea gave rise to a separate language called [Plato](https://github.com/cdiggins/plato) 
that uses a subset of C# syntax, but has pure-functional semantics. 

The Plato tool-chain and core libraries are being written in C# following the Platonic C# guidelines.   

# Platonic Libraries under Development  

* [Plato.Collections](https://github.com/cdiggins/Plato.Collections)
* [Plato.Math](https://github.com/cdiggins/Plato.Math)
* [Parakeet](https://github.com/cdiggins/parakeet)

# Tooling Under Development 

* A Roslyn code analyzer that enforces the Platonic code style 
* An optimizer that converts Platonic C# into efficient byte code
* A Platonic C# => Plato translator 

