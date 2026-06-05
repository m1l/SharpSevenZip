using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SharpSevenZip;

internal static partial class MarkOfTheWeb
{
    private const string ZoneIdentifierStream = ":Zone.Identifier";
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint CreateAlways = 2;
    private const uint FileAttributeNormal = 0x00000080;

    public static byte[]? Read(string? sourceFileName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            string.IsNullOrEmpty(sourceFileName))
        {
            return null;
        }

        try
        {
            using var source = OpenRead(sourceFileName + ZoneIdentifierStream);
            if (source == null)
            {
                return null;
            }

            using var content = new MemoryStream();
            source.CopyTo(content);
            return content.ToArray();
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Apply(byte[]? zoneIdentifier, string destinationFileName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            zoneIdentifier == null ||
            string.IsNullOrEmpty(destinationFileName))
        {
            return;
        }

        try
        {
            using var destination = OpenWrite(destinationFileName + ZoneIdentifierStream);
            if (destination == null)
            {
                return;
            }

            destination.Write(zoneIdentifier, 0, zoneIdentifier.Length);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    internal static bool TryWriteText(string fileName, string content)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        using var stream = OpenWrite(fileName + ZoneIdentifierStream);
        if (stream == null)
        {
            return false;
        }

        using var writer = new StreamWriter(stream);
        writer.Write(content);
        return true;
    }

    internal static bool TryReadText(string fileName, out string content)
    {
        content = "";

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        using var stream = OpenRead(fileName + ZoneIdentifierStream);
        if (stream == null)
        {
            return false;
        }

        using var reader = new StreamReader(stream);
        content = reader.ReadToEnd();
        return true;
    }

    private static FileStream? OpenRead(string fileName)
    {
        return Open(fileName, GenericRead, FileShareRead | FileShareWrite, OpenExisting, FileAccess.Read);
    }

    private static FileStream? OpenWrite(string fileName)
    {
        return Open(fileName, GenericWrite, 0, CreateAlways, FileAccess.Write);
    }

    private static FileStream? Open(string fileName, uint access, uint shareMode, uint creationDisposition, FileAccess fileAccess)
    {
        var handle = CreateFile(fileName, access, shareMode, IntPtr.Zero, creationDisposition, FileAttributeNormal, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            handle.Dispose();
            return null;
        }

        return new FileStream(handle, fileAccess);
    }

#if NET8_0_OR_GREATER
    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);
#else
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);
#endif
}
