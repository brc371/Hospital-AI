namespace Hospital_AI.Models
{
    /// <summary>
    /// The lifecycle status of an encounter's current in-progress note.
    /// </summary>
    public enum EncounterStatus
    {
        /// <summary>The provider has entered a transcript/notes but has not saved a final note yet.</summary>
        Draft = 0,

        /// <summary>The provider has saved at least one finalized note version for this encounter.</summary>
        Saved = 1
    }
}
