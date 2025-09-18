namespace WowheadScraper;

public class IdGenerator
{
    private int _currentId = 0;

    public int GetNextId()
    {
        return Interlocked.Increment(ref _currentId);
    }
}