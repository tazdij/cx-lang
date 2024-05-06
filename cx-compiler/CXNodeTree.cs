using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CXCompiler
{
    public class CXNodeTree<T>
    {

        T value;

        List<CXNodeTree<T>> nodes;
        CXNodeTree<T> parent;

    }
}
