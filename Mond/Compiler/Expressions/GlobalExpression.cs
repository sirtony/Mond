﻿namespace Mond.Compiler.Expressions
{
    class GlobalExpression : Expression, IConstantExpression
    {
        public GlobalExpression(Token token)
            : base(token.FileName, token.Line)
        {
            
        }

        public override void Print(IndentTextWriter writer)
        {
            writer.WriteIndent();
            writer.WriteLine("global");
        }

        public override int Compile(FunctionContext context)
        {
            context.Line(FileName, Line);

            return context.LoadGlobal();
        }

        public override Expression Simplify()
        {
            return this;
        }

        public MondValue GetValue()
        {
            return MondValue.Null;
        }
    }
}
