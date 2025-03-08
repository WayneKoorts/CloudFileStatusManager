namespace CloudFileStatusManager
{
    public interface IStatusManager
    {
        /// <summary>
        /// <para>
        /// Returns the hydration status of the file. Note that a "hydrated" file
        /// may or may not reside within a cloud folder.
        /// </para>
        /// <seealso cref="FileHydrationStatus"/>
        /// </summary>
        FileHydrationStatus GetHydrationStatus();

        FilePinStatus GetPinStatus();

        /// <summary>
        /// <para>
        /// Indicates to the cloud provider that the file should be hydrated. The
        /// file will initially go into the "hydrating" state and then transition
        /// to the "hydrated" state once the download is complete.
        /// </para>
        /// <seealso cref="FileHydrationStatus"/>
        /// </summary>
        /// <param name="filePath"></param>
        void HydrateFile(string filePath);

        /// <summary>
        /// <para>
        /// Indicates to the cloud provider that the local copy of the file should
        /// be deleted and replaced with a placeholder file.
        /// </para>
        /// <para>
        /// Note that there is currently no "dehydrating" status recognised within
        /// this library. This is because I'm not aware of a combination of file
        /// attributes which will indicate that a file is in this state. Luckily
        /// this operation typically occurs quite quickly, and you can infer this
        /// state by knowing that the file was initially hydrated.
        /// </para>
        /// <seealso cref="FileHydrationStatus"/>
        /// </summary>
        /// <param name="filePath"></param>
        void DehydrateFile(string filePath);
    }
}
