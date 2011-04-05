#!/usr/bin/perl -w

unshift @INC, 'support/bytecode';

if( defined &Internals::Net::compile_assembly ) {
    Internals::Net::compile_assembly
        ( 'support/dotnet/bin/Debug/Language.P.Net.Parser',
          qw(Carp::Heavy
             Carp
             constant
             Exporter::Heavy
             Exporter
             Language::P::Assembly
             Language::P::Constants
             Language::P::Exception
             Language::P::Intermediate::Generator
             Language::P::Intermediate::Transform
             Language::P::Keywords
             Language::P::Lexer
             Language::P::Object
             Language::P::Opcodes
             Language::P::Parser::Exception
             Language::P::Parser::Lexicals
             Language::P::Parser::Regex
             Language::P::Parser
             Language::P::ParseTree::PropagateContext
             Language::P::ParseTree::Visitor
             Language::P::ParseTree
             Language::P
             parent
             strict
             subs
             vars
             warnings::register
             warnings
             ) );
}
