using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Avayomi.Generators.Abstractions;

internal abstract class GeneratorStep<T> : GeneratorStep, ISyntaxReceiver
    where T : SyntaxNode
{
    private readonly List<T> _nodes = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not T node)
        {
            return;
        }

        if (Filter(node))
        {
            _nodes.Add(node);
        }
    }

    public override void Execute()
    {
        Execute(_nodes.ToArray());
    }

    public abstract void Execute(T[] declarationSyntaxes);

    public virtual bool Filter(T node) => true;
}
