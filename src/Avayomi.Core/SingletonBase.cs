namespace Avayomi.Core;

public abstract class SingletonBase<TSelf>
    where TSelf : SingletonBase<TSelf>, new()
{
    public static TSelf Instance => Singleton<TSelf>.Instance;
}
