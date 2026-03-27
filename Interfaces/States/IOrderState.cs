namespace API_DigiBook.Interfaces.States
{
    /// <summary>
    /// Contract for order states in the State Pattern
    /// </summary>
    public interface IOrderState
    {
        /// <summary>
        /// Check if the transition to the specified next status is allowed
        /// </summary>
        /// <param name="nextStatus">The target status string</param>
        /// <returns>True if transition is allowed, false otherwise</returns>
        bool CanTransitionTo(string nextStatus);

        /// <summary>
        /// Gets the current status string representation
        /// </summary>
        string GetStatusName();
    }
}
