namespace EntityMongowork
{
    public interface IUnitOfWork
    {
        Task SaveChangesAsync();
    }
}
