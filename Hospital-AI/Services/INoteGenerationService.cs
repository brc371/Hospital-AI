namespace Hospital_AI.Services
{
    /// <summary>
    /// Generates a SOAP note for an encounter by streaming AI-generated text token-by-token,
    /// using tool-calling so the model can pull the patient's prior encounter history as
    /// context when relevant (e.g. for returning patients).
    /// </summary>
    public interface INoteGenerationService
    {
        /// <summary>
        /// Streams the generated SOAP note text for the given encounter. The model is given a
        /// tool it can call to retrieve the patient's prior saved note versions, so it can
        /// reference relevant history (e.g. "continues to report...") for returning patients.
        /// </summary>
        /// <param name="encounterId">The encounter to generate a note for.</param>
        /// <param name="cancellationToken">A token to cancel the streaming generation.</param>
        IAsyncEnumerable<string> GenerateNoteStreamAsync(Guid encounterId, CancellationToken cancellationToken);
    }
}
