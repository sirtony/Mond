﻿namespace Mond.Compiler.Expressions
{
    class EmptyExpression : Expression
    {
        public EmptyExpression(Token token)
            : base(token.FileName, token.Line, token.Column)
        {
            
        }

        public override int Compile(FunctionContext context)
        {
            return 0;
        }

        public override Expression Simplify()
        {
            return this;
        }

        public override T Accept<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
