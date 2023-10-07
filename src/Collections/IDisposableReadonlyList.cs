using System;
using System.Collections.Generic;

namespace Qtl.DisplayCapture.Collections;

public interface IDisposableReadonlyList<T> : IDisposable, IReadOnlyList<T> where T : IDisposable
{

}
