namespace SimpleLauncher.Presentation
{
    public delegate void AddFavoriteServerEventHandler(object sender, string ipAddress, AddFavoriteOrHistoryOperationResult operationResult);
    public delegate void AddHistoryServerEventHandler(object sender, string ipAddress, AddFavoriteOrHistoryOperationResult operationResult);

    public enum AddFavoriteOrHistoryOperationResult
    {
        Success,
        FailedAlreadyExist,
        FailedCannotPing,
        FailedBadAddress,
        FailedNotSet,
        Failed,
        Cancelled,
        InProgress
    }
}
