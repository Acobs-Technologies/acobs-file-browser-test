using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AcobsTechnologies.FileBrowser;
using System.IO;
using NUnit.Framework.Internal;
using UnityEngine.SceneManagement;

using AcobsTechnologies.FileBrowser.Actions;
using System;

namespace Tests
{

    public class TestFileBrowser
    {

        [Test]
        public void TestCreateDirectory()
        {
            string dataPath = GetDataPath();

            string directoryName = "Directory 1";
            string directoryPath = Path.Combine(dataPath, directoryName);

            Assert.False(FileBrowserCore.DirectoryExists(directoryPath), "The directory already exists.");

            directoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:directoryName);

            Assert.True(FileBrowserCore.DirectoryExists(directoryPath));

            FileBrowserCore.DeleteDirectory(directoryPath);

        }

        [UnityTest]
        public IEnumerator TestPasteActionCopyDefault()
        {
            // Test copy with paste action on any platform

            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionCopy(sourceDirectoryParent:testDirPath, targetDirectoryParent:testDirPath);

            FileBrowserCore.DeleteDirectory(path:testDirPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopySAF()
        {
            // Test copy with paste action when using SAF

            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionCopy(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopySAFToAppDir()
        {
            // Test copy with paste action. Copy from SAF directory to app directory (non-SAF)

            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopy(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyAppDirToSAF()
        {
            // Test copy with paste action. Copy from app directory (non-SAF) to SAF directory

            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopy(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        private IEnumerator TestPasteActionCopy(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Copy contents of Source Directory to Target Directory

            // Source Directory
            //   file1.txt
            //   Empty Directory
            //   Directory1
            //     file2.txt
            //     Directory2
            //       file3.txt
            //
            // Target Directory

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:sourceDirectoryPath, name:file1NameSource, text:file1TextSource);

            string emptyDirectoryNameSource = "Empty Directory";
            string emptyDirectoryPathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:emptyDirectoryNameSource);

            string directory1NameSource = "Directory1";
            string directory1PathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:directory1NameSource);

            string file2NameSource = "file2.txt";
            string file2TextSource = "Test file 2";
            string file2PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file2NameSource, text:file2TextSource);

            string directory2NameSource = "Directory2";
            string directory2PathSource = FileBrowserCore.CreateDirectory(parentPath:directory1PathSource, name:directory2NameSource);

            string file3NameSource = "file3.txt";
            string file3TextSource = "Test file 3";
            string file3PathSource = FileBrowserCore.CreateFile(directoryPath:directory2PathSource, name:file3NameSource, text:file3TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            // Check files and directories not already present
            Assert.False(FileBrowserCore.FileExists(
                directoryPath:targetDirectoryPath, 
                fileName:file1NameSource), 
                $"The file {file1NameSource} already exists."
            );
            Assert.False(FileBrowserCore.DirectoryExists(
                parentDirectoryPath:targetDirectoryPath, 
                directoryName:emptyDirectoryNameSource), 
                $"The directory {emptyDirectoryNameSource} already exists."
            );
            Assert.False(FileBrowserCore.DirectoryExists(
                parentDirectoryPath:targetDirectoryPath, 
                directoryName:directory1NameSource), 
                $"The directory {directory1NameSource} already exists."
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                // Check source files and directories still exist

                string file1TextSourceAfterCopy = FileBrowserCore.ReadTextFromFile(filePath:file1PathSource);
                Assert.AreEqual(file1TextSource, file1TextSourceAfterCopy);

                Assert.True(FileBrowserCore.DirectoryExists(path:emptyDirectoryPathSource));

                Assert.True(FileBrowserCore.DirectoryExists(path:directory1PathSource));

                string file2TextSourceAfterCopy = FileBrowserCore.ReadTextFromFile(filePath:file2PathSource);
                Assert.AreEqual(file2TextSource, file2TextSourceAfterCopy);

                Assert.True(FileBrowserCore.DirectoryExists(path:directory2PathSource));

                string file3TextSourceAfterCopy = FileBrowserCore.ReadTextFromFile(filePath:file3PathSource);
                Assert.AreEqual(file3TextSource, file3TextSourceAfterCopy);


                // Check files and directories have been copied to target location

                string file1TextTarget = FileBrowserCore.ReadTextFromFile(directory:targetDirectoryPath, fileName:file1NameSource);
                Assert.AreEqual(file1TextSource, file1TextTarget);
                
                Assert.True(FileBrowserCore.DirectoryExists(parentDirectoryPath:targetDirectoryPath, directoryName:emptyDirectoryNameSource));

                Assert.True(FileBrowserCore.DirectoryExists(parentDirectoryPath:targetDirectoryPath, directoryName:directory1NameSource));

                FileSystemEntry[] fileSystemEntries = FileBrowserCore.GetFileSystemEntriesInDirectory(path:targetDirectoryPath);

                string directory1PathTarget = null;
                for(int i = 0; i < fileSystemEntries.Length; i++){
                    if(fileSystemEntries[i].Name == directory1NameSource){
                        directory1PathTarget = fileSystemEntries[i].Path;
                        break;
                    }
                }

                Assert.NotNull(directory1PathTarget);

                string file2TextTarget = FileBrowserCore.ReadTextFromFile(directory:directory1PathTarget, fileName:file2NameSource);
                Assert.AreEqual(file2TextSource, file2TextTarget);

                Assert.True(FileBrowserCore.DirectoryExists(parentDirectoryPath:directory1PathTarget, directoryName:directory2NameSource));

                fileSystemEntries = FileBrowserCore.GetFileSystemEntriesInDirectory(path:directory1PathTarget);

                string directory2PathTarget = null;
                for(int i = 0; i < fileSystemEntries.Length; i++){
                    if(fileSystemEntries[i].Name == directory2NameSource){
                        directory2PathTarget = fileSystemEntries[i].Path;
                        break;
                    }
                }

                Assert.NotNull(directory2PathTarget);

                string file3TextTarget = FileBrowserCore.ReadTextFromFile(directory:directory2PathTarget, fileName:file3NameSource);
                Assert.AreEqual(file3TextSource, file3TextTarget);

                pasteActionFinished = true;
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:true);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionCopyFileExistsAbortDefault()
        {
            // Copy a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.

            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirectoryName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirectoryName);

            yield return TestPasteActionCopyFileExistsAbort(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyFileExistsAbortSAF()
        {
            // Copy a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.
            // Source and target SAF.

            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionCopyFileExistsAbort(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyFileExistsAbortSAFToAppDir()
        {
            // Copy a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.
            // Source SAF, target in App Diretory.

            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopyFileExistsAbort(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyFileExistsAbortAppDirToSAF()
        {
            // Copy a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.
            // Source in App Directory, target SAF.

            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopyFileExistsAbort(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }


        private IEnumerator TestPasteActionCopyFileExistsAbort(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Copy a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:sourceDirectoryPath, name:file1NameSource, text:file1TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            string file1NameExistingAtTarget = "file1.txt";
            string file1TextExistingAtTarget = "Test file 1 target text";
            string file1PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:targetDirectoryPath, 
                name:file1NameExistingAtTarget, 
                text:file1TextExistingAtTarget
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileExists){
                        action.SkipCurrentFileSystemEntry();
                    }else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check file at target location has not been overwritten

                    string file1TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file1PathExistingAtTarget);
                    Assert.AreEqual(file1TextExistingAtTarget, file1TextTarget);

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:true);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionCopyFileExistsOverwriteDefault()
        {
            // Copy a file to a location where a file with this name already exists. 
            // Overwrite the existing file.

            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionCopyFileExistsOverwrite(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyFileExistsOverwriteSAF()
        {
            // Copy a file to a location where a file with this name already exists. 
            // Overwrite the existing file.
            // Source and target SAF.

            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionCopyFileExistsOverwrite(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyFileExistsOverwriteSAFToAppDir()
        {
            // Copy a file to a location where a file with this name already exists. 
            // Overwrite the existing file.
            // Source SAF, target in app directory.

            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopyFileExistsOverwrite(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyFileExistsOverwriteAppDirToSAF()
        {
            // Copy a file to a location where a file with this name already exists. 
            // Overwrite the existing file.
            // Source in app directory, target SAF.

            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopyFileExistsOverwrite(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        private IEnumerator TestPasteActionCopyFileExistsOverwrite(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Copy a file to a location where a file with this name already exists. 
            // Overwrite the existing file.

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:sourceDirectoryPath, name:file1NameSource, text:file1TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            string file1NameExistingAtTarget = "file1.txt";
            string file1TextExistingAtTarget = "Test file 1 target text";
            string file1PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:targetDirectoryPath, 
                name:file1NameExistingAtTarget, 
                text:file1TextExistingAtTarget
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileExists){
                        action.RetryCurrentFileSystemEntry(overwrite:true);
                    }else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check file at target location has been overwritten

                    string file1TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file1PathExistingAtTarget);
                    Assert.AreEqual(file1TextSource, file1TextTarget);

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:true);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionCopyDirectoryExistsAbortDefault()
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //
            //  Target Directory
            //    Directory1
            //     file2.txt


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionCopyDirectoryExistsAbort(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyDirectoryExistsAbortSAF()
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Test Directory (SAF directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //
            //   Target Directory
            //     Directory1
            //       file2.txt


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionCopyDirectoryExistsAbort(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyDirectoryExistsAbortSAFToAppDir()
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Test Directory (SAF directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //
            // Test Directory (Inside app directory)
            //   Target Directory
            //     Directory1
            //       file2.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopyDirectoryExistsAbort(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyDirectoryExistsAbortAppDirToSAF()
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Test Directory (Inside app directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //
            // Test Directory (SAF directory)
            //   Target Directory
            //     Directory1
            //       file2.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopyDirectoryExistsAbort(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }


        private IEnumerator TestPasteActionCopyDirectoryExistsAbort(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //
            //  Target Directory
            //    Directory1
            //     file2.txt

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string directory1NameSource = "Directory1";
            string directory1PathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:directory1NameSource);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file1NameSource, text:file1TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            string directory1NameTarget = "Directory1";
            string directory1PathTarget= FileBrowserCore.CreateDirectory(parentPath:targetDirectoryPath, name:directory1NameTarget);

            string file2NameExistingAtTarget = "file2.txt";
            string file2TextExistingAtTarget = "Test file 2 target text";
            string file2PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:targetDirectoryPath, 
                name:file2NameExistingAtTarget, 
                text:file2TextExistingAtTarget
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentDirectoryExists){
                        action.SkipCurrentFileSystemEntry();
                    }else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check directory and its contents at target location have not been overwritten

                    string file2TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file2PathExistingAtTarget);
                    Assert.AreEqual(file2TextExistingAtTarget, file2TextTarget);

                    Assert.False(FileBrowserCore.FileExists(directoryPath:directory1PathTarget, fileName:file1NameSource));

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:true);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionCopyDirectoryExistsMergeDefault()
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     file2.txt
            //     file3.txt
            //
            //  Target Directory
            //    Directory1
            //     file1.txt <- overwrite
            //     file2.txt <- keep
            //               <- add file3.txt


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionCopyDirectoryExistsMerge(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyDirectoryExistsMergeSAF()
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Test Directory (SAF directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //       file2.txt
            //       file3.txt
            //
            //    Target Directory
            //      Directory1
            //       file1.txt <- overwrite
            //       file2.txt <- keep
            //                 <- add file3.txt


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionCopyDirectoryExistsMerge(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyDirectoryExistsMergeSAFToAppDir()
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Test Directory (SAF directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //       file2.txt
            //       file3.txt
            //
            // Test Directory (Inside App directory)
            //    Target Directory
            //      Directory1
            //       file1.txt <- overwrite
            //       file2.txt <- keep
            //                 <- add file3.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopyDirectoryExistsMerge(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyDirectoryExistsMergeAppDirToSAF()
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Test Directory (Inside App directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //       file2.txt
            //       file3.txt
            //
            // Test Directory (SAF directory)
            //    Target Directory
            //      Directory1
            //       file1.txt <- overwrite
            //       file2.txt <- keep
            //                 <- add file3.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionCopyDirectoryExistsMerge(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        private IEnumerator TestPasteActionCopyDirectoryExistsMerge(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Copy a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     file2.txt
            //     file3.txt
            //
            //  Target Directory
            //    Directory1
            //     file1.txt <- overwrite
            //     file2.txt <- keep
            //               <- add file3.txt

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string directory1NameSource = "Directory1";
            string directory1PathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:directory1NameSource);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file1NameSource, text:file1TextSource);

            string file2NameSource = "file2.txt";
            string file2TextSource = "Test file 2 source text";
            string file2PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file2NameSource, text:file2TextSource);

            string file3NameSource = "file3.txt";
            string file3TextSource = "Test file 3 source text";
            string file3PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file3NameSource, text:file3TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            string directory1NameTarget = "Directory1";
            string directory1PathTarget= FileBrowserCore.CreateDirectory(parentPath:targetDirectoryPath, name:directory1NameTarget);

            string file1NameExistingAtTarget = "file1.txt";
            string file1TextExistingAtTarget = "Test file 1 target text";
            string file1PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:directory1PathTarget, 
                name:file1NameExistingAtTarget, 
                text:file1TextExistingAtTarget
            );

            string file2NameExistingAtTarget = "file2.txt";
            string file2TextExistingAtTarget = "Test file 2 target text";
            string file2PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:directory1PathTarget, 
                name:file2NameExistingAtTarget, 
                text:file2TextExistingAtTarget
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentDirectoryExists){
                        action.RetryCurrentDirectory(mode:FileBrowserPasteAction.HandleExistingDirectoryMode.Merge);

                    }else if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileExists){

                        if(action.Files[0].sourceName == file1NameSource){
                            // Overwrite
                            action.RetryCurrentFileSystemEntry(overwrite:true);
                        }else if(action.Files[0].sourceName == file2NameSource){
                            // Keep existing file
                            action.SkipCurrentFileSystemEntry();
                        }else{
                            Assert.Fail("Paste action was interrupted due to an unknown error.");

                            pasteActionFinished = true;
                        }
                    }
                    else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check directories have been merged

                    string file1TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file1PathExistingAtTarget);
                    Assert.AreEqual(file1TextSource, file1TextTarget);

                    string file2TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file2PathExistingAtTarget);
                    Assert.AreEqual(file2TextExistingAtTarget, file2TextTarget);

                    string file3TextTarget = FileBrowserCore.ReadTextFromFile(directory:directory1PathTarget, fileName:file3NameSource);
                    Assert.AreEqual(file3TextSource, file3TextTarget);

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:true);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionCopyInSubDirectoryDefault()
        {
            // Attempt to copy a directory into its own subdirectory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     Directory2
            //                <- copy Directory1 in here


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionCopyInSubDirectory(sourceDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyInSubDirectorySAF()
        {
            // Attempt to copy a directory into its own subdirectory.

            // Test Directory (SAF directory)
            //   Source Directory 
            //     Directory1
            //       file1.txt
            //       Directory2
            //                  <- copy Directory1 in here


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionCopyInSubDirectory(sourceDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        private IEnumerator TestPasteActionCopyInSubDirectory(string sourceDirectoryParent)
        {
            // Attempt to copy a directory into its own subdirectory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     Directory2
            //                <- copy Directory1 in here

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string directory1NameSource = "Directory1";
            string directory1PathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:directory1NameSource);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file1NameSource, text:file1TextSource);

            string directory2NameSource = "Directory2";
            string directory2PathSource = FileBrowserCore.CreateDirectory(parentPath:directory1PathSource, name:directory2NameSource);



            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            bool targetIsSubdirectoryDetected = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.TargetPathIsSubdirectory){
                        targetIsSubdirectoryDetected = true;
                        action.SkipCurrentFileSystemEntry();
                    }else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check Directory1 is not copied into Directory2
                    Assert.False(FileBrowserCore.DirectoryExists(
                        parentDirectoryPath:directory2PathSource, 
                        directoryName:directory1NameSource)
                    );

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:true);

            fb.PasteClipboard(targetDirectory:directory2PathSource);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            Assert.True(targetIsSubdirectoryDetected);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionCopyIdenticalPathDefault()
        {
            // Attempt to copy and overwrite a file at a target path that is identical to the path of the file.

            // Source Directory
            //   file1.txt


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionCopyIdenticalPath(sourceDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionCopyIdenticalPathSAF()
        {
            // Attempt to copy and overwrite a file at a target path that is identical to the path of the file.

            // Test Directory (SAF directory)
            //   Source Directory
            //     file1.txt


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionCopyIdenticalPath(sourceDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        private IEnumerator TestPasteActionCopyIdenticalPath(string sourceDirectoryParent)
        {
            // Attempt to copy and overwrite a file at a target path that is identical to the path of the file.

            // Source Directory
            //   file1.txt


            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:sourceDirectoryPath, name:file1NameSource, text:file1TextSource);


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            bool identicalPathsDetected = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileExists){
                        action.RetryCurrentFileSystemEntry(overwrite:true);
                    }else if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileSystemEntryError){
                        if(action.CurrentFileSystemEntryError == FileBrowserCore.FileSystemOperationResult.IdenticalPaths){
                            identicalPathsDetected = true;
                            action.SkipCurrentFileSystemEntry();
                        }
                    }
                    else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check File1 still exists
                    string file1Text = FileBrowserCore.ReadTextFromFile(filePath:file1PathSource);
                    Assert.AreEqual(file1TextSource, file1Text);

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:true);

            fb.PasteClipboard(targetDirectory:sourceDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            Assert.True(identicalPathsDetected);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
        }



        [UnityTest]
        public IEnumerator TestPasteActionMoveDefault()
        {
            // Move contents of Source Directory to Target Directory

            // Source Directory
            //   file1.txt
            //   Empty Directory
            //   Directory1
            //     file2.txt
            //     Directory2
            //       file3.txt
            //
            // Target Directory



            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionMove(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveSAF()
        {
            // Move contents of Source Directory to Target Directory

            // Source Directory
            //   file1.txt
            //   Empty Directory
            //   Directory1
            //     file2.txt
            //     Directory2
            //       file3.txt
            //
            // Target Directory


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionMove(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveSAFToAppDir()
        {
            // Move contents of Source Directory to Target Directory

            // Test Directory (SAF directory)
            //   Source Directory
            //     file1.txt
            //     Empty Directory
            //     Directory1
            //       file2.txt
            //       Directory2
            //         file3.txt
            //
            // Test Directory (inside app directory)
            //   Target Directory


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMove(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveAppDirToSAF()
        {
            // Move contents of Source Directory to Target Directory

            // Test Directory (inside app directory)
            //   Source Directory
            //     file1.txt
            //     Empty Directory
            //     Directory1
            //       file2.txt
            //       Directory2
            //         file3.txt
            //
            // Test Directory (SAF directory)
            //   Target Directory


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMove(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        private IEnumerator TestPasteActionMove(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Move contents of Source Directory to Target Directory

            // Source Directory
            //   file1.txt
            //   Empty Directory
            //   Directory1
            //     file2.txt
            //     Directory2
            //       file3.txt
            //
            // Target Directory

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:sourceDirectoryPath, name:file1NameSource, text:file1TextSource);

            string emptyDirectoryNameSource = "Empty Directory";
            string emptyDirectoryPathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:emptyDirectoryNameSource);

            string directory1NameSource = "Directory1";
            string directory1PathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:directory1NameSource);

            string file2NameSource = "file2.txt";
            string file2TextSource = "Test file 2";
            string file2PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file2NameSource, text:file2TextSource);

            string directory2NameSource = "Directory2";
            string directory2PathSource = FileBrowserCore.CreateDirectory(parentPath:directory1PathSource, name:directory2NameSource);

            string file3NameSource = "file3.txt";
            string file3TextSource = "Test file 3";
            string file3PathSource = FileBrowserCore.CreateFile(directoryPath:directory2PathSource, name:file3NameSource, text:file3TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            // Check files and directories not already present
            Assert.False(FileBrowserCore.FileExists(
                directoryPath:targetDirectoryPath, 
                fileName:file1NameSource), 
                $"The file {file1NameSource} already exists."
            );
            Assert.False(FileBrowserCore.DirectoryExists(
                parentDirectoryPath:targetDirectoryPath, 
                directoryName:emptyDirectoryNameSource), 
                $"The directory {emptyDirectoryNameSource} already exists."
            );
            Assert.False(FileBrowserCore.DirectoryExists(
                parentDirectoryPath:targetDirectoryPath, 
                directoryName:directory1NameSource), 
                $"The directory {directory1NameSource} already exists."
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                // Check source files and directories do not exist anymore in source directory

                Assert.False(FileBrowserCore.FileExists(path:file1PathSource));

                Assert.False(FileBrowserCore.DirectoryExists(path:emptyDirectoryPathSource));

                Assert.False(FileBrowserCore.DirectoryExists(path:directory1PathSource));

                // Check files and directories have been moved to target location

                string file1TextTarget = FileBrowserCore.ReadTextFromFile(directory:targetDirectoryPath, fileName:file1NameSource);
                Assert.AreEqual(file1TextSource, file1TextTarget);
                
                Assert.True(FileBrowserCore.DirectoryExists(parentDirectoryPath:targetDirectoryPath, directoryName:emptyDirectoryNameSource));

                Assert.True(FileBrowserCore.DirectoryExists(parentDirectoryPath:targetDirectoryPath, directoryName:directory1NameSource));

                FileSystemEntry[] fileSystemEntries = FileBrowserCore.GetFileSystemEntriesInDirectory(path:targetDirectoryPath);

                string directory1PathTarget = null;
                for(int i = 0; i < fileSystemEntries.Length; i++){
                    if(fileSystemEntries[i].Name == directory1NameSource){
                        directory1PathTarget = fileSystemEntries[i].Path;
                        break;
                    }
                }

                Assert.NotNull(directory1PathTarget);

                string file2TextTarget = FileBrowserCore.ReadTextFromFile(directory:directory1PathTarget, fileName:file2NameSource);
                Assert.AreEqual(file2TextSource, file2TextTarget);

                Assert.True(FileBrowserCore.DirectoryExists(parentDirectoryPath:directory1PathTarget, directoryName:directory2NameSource));

                fileSystemEntries = FileBrowserCore.GetFileSystemEntriesInDirectory(path:directory1PathTarget);

                string directory2PathTarget = null;
                for(int i = 0; i < fileSystemEntries.Length; i++){
                    if(fileSystemEntries[i].Name == directory2NameSource){
                        directory2PathTarget = fileSystemEntries[i].Path;
                        break;
                    }
                }

                Assert.NotNull(directory2PathTarget);

                string file3TextTarget = FileBrowserCore.ReadTextFromFile(directory:directory2PathTarget, fileName:file3NameSource);
                Assert.AreEqual(file3TextSource, file3TextTarget);

                pasteActionFinished = true;
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:false);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionMoveFileExistsAbortDefault()
        {
            // Move a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.

            // Source Directory
            //   file1.txt
            //
            // Target Directory
            //   file1.txt


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionMoveFileExistsAbort(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveFileExistsAbortSAF()
        {
            // Move a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.

            // Source Directory
            //   file1.txt
            //
            // Target Directory
            //   file1.txt


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionMoveFileExistsAbort(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveFileExistsAbortSAFToAppDir()
        {
            // Move a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.

            // Test Directory (SAF directory)
            //   Source Directory
            //     file1.txt
            //
            // Test Directory (inside app directory)
            //   Target Directory
            //     file1.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMoveFileExistsAbort(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveFileExistsAbortAppDirToSAF()
        {
            // Move a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.

            // Test Directory (inside app directory)
            //   Source Directory
            //     file1.txt
            //
            // Test Directory (SAF directory)
            //   Target Directory
            //     file1.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMoveFileExistsAbort(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        private IEnumerator TestPasteActionMoveFileExistsAbort(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Move a file to a location where a file with this name already exists. 
            // Cancel and do not overwrite the existing file.

            // Source Directory
            //   file1.txt
            //
            // Target Directory
            //   file1.txt

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:sourceDirectoryPath, name:file1NameSource, text:file1TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            string file1NameExistingAtTarget = "file1.txt";
            string file1TextExistingAtTarget = "Test file 1 target text";
            string file1PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:targetDirectoryPath, 
                name:file1NameExistingAtTarget, 
                text:file1TextExistingAtTarget
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileExists){
                        action.SkipCurrentFileSystemEntry();
                    }else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check file at target location has not been overwritten

                    string file1TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file1PathExistingAtTarget);
                    Assert.AreEqual(file1TextExistingAtTarget, file1TextTarget);
                    
                    // Check file at source location still exists

                    string file1TextSourceAfterMove = FileBrowserCore.ReadTextFromFile(filePath:file1PathSource);
                    Assert.AreEqual(file1TextSource, file1TextSourceAfterMove);

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:false);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionMoveFileExistsOverwriteDefault()
        {
            // Move a file to a location where a file with this name already exists. 
            // Overwrite the existing file.

            // Source Directory
            //   file1.txt
            //
            // Target Directory
            //   file1.txt


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionMoveFileExistsOverwrite(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveFileExistsOverwriteSAF()
        {
            // Move a file to a location where a file with this name already exists. 
            // Overwrite the existing file.

            // Source Directory
            //   file1.txt
            //
            // Target Directory
            //   file1.txt


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionMoveFileExistsOverwrite(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveFileExistsOverwriteSAFToAppDir()
        {
            // Move a file to a location where a file with this name already exists. 
            // Overwrite the existing file.

            // Test Directory (SAF directory)
            //   Source Directory
            //     file1.txt
            //
            // Test Directory (inside app directory)
            //   Target Directory
            //     file1.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMoveFileExistsOverwrite(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveFileExistsOverwriteAppDirToSAF()
        {
            // Move a file to a location where a file with this name already exists. 
            // Overwrite the existing file.

            // Test Directory (inside app directory)
            //   Source Directory
            //     file1.txt
            //
            // Test Directory (SAF directory)
            //   Target Directory
            //     file1.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMoveFileExistsOverwrite(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        private IEnumerator TestPasteActionMoveFileExistsOverwrite(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Move a file to a location where a file with this name already exists. 
            // Overwrite the existing file.

            // Source Directory
            //   file1.txt
            //
            // Target Directory
            //   file1.txt

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:sourceDirectoryPath, name:file1NameSource, text:file1TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            string file1NameExistingAtTarget = "file1.txt";
            string file1TextExistingAtTarget = "Test file 1 target text";
            string file1PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:targetDirectoryPath, 
                name:file1NameExistingAtTarget, 
                text:file1TextExistingAtTarget
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileExists){
                        action.RetryCurrentFileSystemEntry(overwrite:true);
                    }else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check file at source location does not exist anymore
                    Assert.False(FileBrowserCore.FileExists(path:file1PathSource));

                    // Check file at target location has been overwritten
                    string file1TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file1PathExistingAtTarget);
                    Assert.AreEqual(file1TextSource, file1TextTarget);
                    
                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:false);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionMoveDirectoryExistsAbortDefault()
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //
            //  Target Directory
            //    Directory1
            //     file2.txt


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionMoveDirectoryExistsAbort(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveDirectoryExistsAbortSAF()
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //
            //  Target Directory
            //    Directory1
            //     file2.txt


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionMoveDirectoryExistsAbort(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveDirectoryExistsAbortSAFToAppDir()
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Test Directory (SAF directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //
            // Test Directory (inside app directory)
            //   Target Directory
            //     Directory1
            //      file2.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMoveDirectoryExistsAbort(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveDirectoryExistsAbortAppDirToSAF()
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Test Directory (inside app directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //
            // Test Directory (SAF directory)
            //   Target Directory
            //     Directory1
            //      file2.txt


            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMoveDirectoryExistsAbort(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        private IEnumerator TestPasteActionMoveDirectoryExistsAbort(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Cancel and do not overwrite the existing directory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //
            //  Target Directory
            //    Directory1
            //     file2.txt

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string directory1NameSource = "Directory1";
            string directory1PathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:directory1NameSource);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file1NameSource, text:file1TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            string directory1NameTarget = "Directory1";
            string directory1PathTarget= FileBrowserCore.CreateDirectory(parentPath:targetDirectoryPath, name:directory1NameTarget);

            string file2NameExistingAtTarget = "file2.txt";
            string file2TextExistingAtTarget = "Test file 2 target text";
            string file2PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:targetDirectoryPath, 
                name:file2NameExistingAtTarget, 
                text:file2TextExistingAtTarget
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentDirectoryExists){
                        action.SkipCurrentFileSystemEntry();
                    }else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check directory and its contents still exist at source location

                    string file1TextSourceAfterMove = FileBrowserCore.ReadTextFromFile(filePath:file1PathSource);
                    Assert.AreEqual(file1TextSource, file1TextSourceAfterMove);

                    // Check directory and its contents at target location have not been overwritten

                    string file2TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file2PathExistingAtTarget);
                    Assert.AreEqual(file2TextExistingAtTarget, file2TextTarget);

                    Assert.False(FileBrowserCore.FileExists(directoryPath:directory1PathTarget, fileName:file1NameSource));

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:false);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionMoveDirectoryExistsMergeDefault()
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     file2.txt
            //     file3.txt
            //
            //  Target Directory
            //    Directory1
            //     file1.txt <- overwrite
            //     file2.txt <- keep
            //               <- add file3.txt

            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionMoveDirectoryExistsMerge(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveDirectoryExistsMergeSAF()
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     file2.txt
            //     file3.txt
            //
            //  Target Directory
            //    Directory1
            //     file1.txt <- overwrite
            //     file2.txt <- keep
            //               <- add file3.txt

            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionMoveDirectoryExistsMerge(sourceDirectoryParent:testDirectoryPath, targetDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveDirectoryExistsMergeSAFToAppDir()
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Test Directory (SAF directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //       file2.txt
            //       file3.txt
            //
            // Test Directory (inside app directory)
            //   Target Directory
            //     Directory1
            //       file1.txt <- overwrite
            //       file2.txt <- keep
            //                 <- add file3.txt

            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMoveDirectoryExistsMerge(sourceDirectoryParent:safTestDirectoryPath, targetDirectoryParent:appDirTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveDirectoryExistsMergeAppDirToSAF()
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Test Directory (inside app directory)
            //   Source Directory
            //     Directory1
            //       file1.txt
            //       file2.txt
            //       file3.txt
            //
            // Test Directory (SAF directory)
            //   Target Directory
            //     Directory1
            //       file1.txt <- overwrite
            //       file2.txt <- keep
            //                 <- add file3.txt

            string safTestDirectoryPath = null;

            string dataPath = GetDataPath();
            string appDirTestDirectoryName = "Test Directory";
            string appDirTestDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:appDirTestDirectoryName);

            yield return GetSAFTestDirectory(callback: (string path) => {safTestDirectoryPath = path;});

            yield return TestPasteActionMoveDirectoryExistsMerge(sourceDirectoryParent:appDirTestDirectoryPath, targetDirectoryParent:safTestDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:safTestDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:appDirTestDirectoryPath);
        }

        private IEnumerator TestPasteActionMoveDirectoryExistsMerge(string sourceDirectoryParent, string targetDirectoryParent)
        {
            // Move a directory to a location where a directory with this name already exists. 
            // Merge the directories.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     file2.txt
            //     file3.txt
            //
            //  Target Directory
            //    Directory1
            //     file1.txt <- overwrite
            //     file2.txt <- keep
            //               <- add file3.txt

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string directory1NameSource = "Directory1";
            string directory1PathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:directory1NameSource);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file1NameSource, text:file1TextSource);

            string file2NameSource = "file2.txt";
            string file2TextSource = "Test file 2 source text";
            string file2PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file2NameSource, text:file2TextSource);

            string file3NameSource = "file3.txt";
            string file3TextSource = "Test file 3 source text";
            string file3PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file3NameSource, text:file3TextSource);

            // Setup target
            string targetDirName = "Target Directory";
            string targetDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:targetDirectoryParent, name:targetDirName);

            string directory1NameTarget = "Directory1";
            string directory1PathTarget= FileBrowserCore.CreateDirectory(parentPath:targetDirectoryPath, name:directory1NameTarget);

            string file1NameExistingAtTarget = "file1.txt";
            string file1TextExistingAtTarget = "Test file 1 target text";
            string file1PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:directory1PathTarget, 
                name:file1NameExistingAtTarget, 
                text:file1TextExistingAtTarget
            );

            string file2NameExistingAtTarget = "file2.txt";
            string file2TextExistingAtTarget = "Test file 2 target text";
            string file2PathExistingAtTarget = FileBrowserCore.CreateFile(
                directoryPath:directory1PathTarget, 
                name:file2NameExistingAtTarget, 
                text:file2TextExistingAtTarget
            );


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentDirectoryExists){
                        action.RetryCurrentDirectory(mode:FileBrowserPasteAction.HandleExistingDirectoryMode.Merge);

                    }else if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileExists){

                        if(action.Files[0].sourceName == file1NameSource){
                            // Overwrite
                            action.RetryCurrentFileSystemEntry(overwrite:true);
                        }else if(action.Files[0].sourceName == file2NameSource){
                            // Keep existing file
                            action.SkipCurrentFileSystemEntry();
                        }else{
                            Assert.Fail("Paste action was interrupted due to an unknown error.");

                            pasteActionFinished = true;
                        }
                    }
                    else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check moved files do not exist at source location anymore
                    Assert.False(FileBrowserCore.FileExists(path:file1PathSource));
                    Assert.False(FileBrowserCore.FileExists(path:file3PathSource));

                    // Check file2.txt that has not been moved still exists at source location
                    Assert.True(FileBrowserCore.FileExists(path:file2PathSource));

                    // Check directories have been merged

                    // Overwritten
                    string file1TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file1PathExistingAtTarget);
                    Assert.AreEqual(file1TextSource, file1TextTarget);

                    // Not overwritten
                    string file2TextTarget = FileBrowserCore.ReadTextFromFile(filePath:file2PathExistingAtTarget);
                    Assert.AreEqual(file2TextExistingAtTarget, file2TextTarget);

                    // Added
                    string file3TextTarget = FileBrowserCore.ReadTextFromFile(directory:directory1PathTarget, fileName:file3NameSource);
                    Assert.AreEqual(file3TextSource, file3TextTarget);

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:false);

            fb.PasteClipboard(targetDirectory:targetDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
            FileBrowserCore.DeleteDirectory(path:targetDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionMoveInSubDirectoryDefault()
        {
            // Attempt to move a directory into its own subdirectory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     Directory2
            //                <- move Directory1 in here


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionMoveInSubDirectory(sourceDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveInSubDirectorySAF()
        {
            // Attempt to move a directory into its own subdirectory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     Directory2
            //                <- move Directory1 in here


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionMoveInSubDirectory(sourceDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        private IEnumerator TestPasteActionMoveInSubDirectory(string sourceDirectoryParent)
        {
            // Attempt to move a directory into its own subdirectory.

            // Source Directory
            //   Directory1
            //     file1.txt
            //     Directory2
            //                <- move Directory1 in here

            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string directory1NameSource = "Directory1";
            string directory1PathSource = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryPath, name:directory1NameSource);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:directory1PathSource, name:file1NameSource, text:file1TextSource);

            string directory2NameSource = "Directory2";
            string directory2PathSource = FileBrowserCore.CreateDirectory(parentPath:directory1PathSource, name:directory2NameSource);


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            bool targetIsSubdirectoryDetected = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.TargetPathIsSubdirectory){
                        targetIsSubdirectoryDetected = true;
                        action.SkipCurrentFileSystemEntry();
                    }else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check Directory1 is not copied into Directory2
                    Assert.False(FileBrowserCore.DirectoryExists(
                        parentDirectoryPath:directory2PathSource, 
                        directoryName:directory1NameSource)
                    );

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:false);

            fb.PasteClipboard(targetDirectory:directory2PathSource);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            Assert.True(targetIsSubdirectoryDetected);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
        }

        [UnityTest]
        public IEnumerator TestPasteActionMoveIdenticalPathDefault()
        {
            // Attempt to move and overwrite a file at a target path that is identical to the path of the file.

            // Source Directory
            //   file1.txt


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestPasteActionMoveIdenticalPath(sourceDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestPasteActionMoveIdenticalPathSAF()
        {
            // Attempt to move and overwrite a file at a target path that is identical to the path of the file.

            // Source Directory
            //   file1.txt


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestPasteActionMoveIdenticalPath(sourceDirectoryParent:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        private IEnumerator TestPasteActionMoveIdenticalPath(string sourceDirectoryParent)
        {
            // Attempt to move and overwrite a file at a target path that is identical to the path of the file.

            // Source Directory
            //   file1.txt


            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup source
            string sourceDirectoryName = "Source Directory";
            string sourceDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:sourceDirectoryParent, name:sourceDirectoryName);

            string file1NameSource = "file1.txt";
            string file1TextSource = "Test file 1 source text";
            string file1PathSource = FileBrowserCore.CreateFile(directoryPath:sourceDirectoryPath, name:file1NameSource, text:file1TextSource);


            fb.Open(initialPath:sourceDirectoryPath);

            bool pasteActionFinished = false;

            bool identicalPathsDetected = false;

            fb.OnActionListChangedEvent += () => {
                fb.CurrentPasteActions[0].OnInterruptedEvent += (FileBrowserPasteAction action) => {
                    if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileExists){
                        action.RetryCurrentFileSystemEntry(overwrite:true);
                    }else if(action.State == FileBrowserPasteAction.PasteActionState.CurrentFileSystemEntryError){
                        if(action.CurrentFileSystemEntryError == FileBrowserCore.FileSystemOperationResult.IdenticalPaths){
                            identicalPathsDetected = true;
                            action.SkipCurrentFileSystemEntry();
                        }
                    }
                    else{
                        Assert.Fail("Paste action was interrupted due to an unknown error.");

                        pasteActionFinished = true;
                    }
                };

                fb.CurrentPasteActions[0].OnFinishedEvent += (FileBrowserPasteAction action) => {

                    // Check File1 still exists
                    string file1Text = FileBrowserCore.ReadTextFromFile(filePath:file1PathSource);
                    Assert.AreEqual(file1TextSource, file1Text);

                    pasteActionFinished = true;
                };
            };

            fb.SelectAllFileSystemEntries();

            fb.SelectedFileSystemEntriesToClipboard(copy:false);

            fb.PasteClipboard(targetDirectory:sourceDirectoryPath);

            
            yield return new WaitWhile(() => pasteActionFinished == false);

            Assert.True(identicalPathsDetected);

            FileBrowserCore.DeleteDirectory(path:sourceDirectoryPath);
        }


        [UnityTest]
        public IEnumerator TestDeleteActionFileDefault()
        {
            // Delete a file with a DeleteAction

            // Directory1
            //   file1.txt <- delete
            //   file1.jpg
            //   file2.txt
            //   file1 (is a directory)


            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestDeleteActionFile(testDirectory:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestDeleteActionSAF()
        {
            // Delete a file with a DeleteAction

            // Directory1
            //   file1.txt <- delete
            //   file1.jpg
            //   file2.txt
            //   file1 (is a directory)


            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestDeleteActionFile(testDirectory:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        private IEnumerator TestDeleteActionFile(string testDirectory)
        {
            // Delete a file with a DeleteAction

            // Directory1
            //   file1.txt <- delete
            //   file1.jpg
            //   file2.txt
            //   file1 (is a directory)


            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup
            string directory1Name = "Directory1";
            string directory1Path = FileBrowserCore.CreateDirectory(parentPath:testDirectory, name:directory1Name);

            string file1Name = "file1.txt";
            string file1Text = "Test file 1";
            string file1Path = FileBrowserCore.CreateFile(directoryPath:directory1Path, name:file1Name, text:file1Text);

            string file1JPGName = "file1.jpg";
            string file1JPGPath = FileBrowserCore.CreateFile(directoryPath:directory1Path, name:file1JPGName);

            string file2Name = "file2.txt";
            string file2Text = "Test file 2";
            string file2Path = FileBrowserCore.CreateFile(directoryPath:directory1Path, name:file2Name, text:file2Text);

            string file1DirectoryName = "file1";
            string file1DirectoryPath = FileBrowserCore.CreateDirectory(parentPath:directory1Path, name:file1DirectoryName);


            fb.Open(initialPath:directory1Path);

            bool deleteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                if(fb.Actions.Count > 0 && fb.Actions[0] is FileBrowserDeleteAction deleteAction){
                    deleteAction.OnFinishedEvent += (FileBrowserDeleteAction action) => {

                        // Check file1.txt was deleted
                        Assert.False(FileBrowserCore.FileExists(path:file1Path));

                        // Check other files and directories still exist

                        Assert.True(FileBrowserCore.FileExists(path:file1JPGPath));

                        string file2TextAfterDelete = FileBrowserCore.ReadTextFromFile(filePath:file2Path);
                        Assert.AreEqual(file2Text, file2TextAfterDelete);

                        Assert.True(FileBrowserCore.DirectoryExists(path:file1DirectoryPath));

                        deleteActionFinished = true;
                    };

                    deleteAction.Start();
                }
            };

            FileSystemEntry[] fileSystemEntries = FileBrowserCore.GetFileSystemEntriesInDirectory(path:directory1Path);

            for(int i = 0; i < fileSystemEntries.Length; i++){
                if(fileSystemEntries[i].Name == file1Name){
                    
                    fb.DeleteFileSystemEntry(fileSystemEntry:fileSystemEntries[i]);

                    break;
                }
            }

            
            yield return new WaitWhile(() => deleteActionFinished == false);

            FileBrowserCore.DeleteDirectory(path:directory1Path);
        }

        [UnityTest]
        public IEnumerator TestDeleteActionDirectoryDefault()
        {
            // Delete a directory with a DeleteAction

            // Directory1 <- delete
            //   file1.txt
            //   Directory2
            //     file2.txt
            // Directory1.txt <- keep
            // Directory3 <- keep



            // Create a temporary directory to test in
            string dataPath = GetDataPath();
            string testDirName = "Test Directory";
            string testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:dataPath, name:testDirName);

            yield return TestDeleteActionDirectory(testDirectory:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        [UnityTest]
        [UnityPlatform (RuntimePlatform.Android)]
        public IEnumerator TestDeleteActionDirectorySAF()
        {
            // Delete a directory with a DeleteAction

            // Directory1 <- delete
            //   file1.txt
            //   Directory2
            //     file2.txt
            // Directory1.txt <- keep
            // Directory3 <- keep

            string testDirectoryPath = null;

            yield return GetSAFTestDirectory(callback: (string path) => {testDirectoryPath = path;});

            yield return TestDeleteActionDirectory(testDirectory:testDirectoryPath);

            FileBrowserCore.DeleteDirectory(path:testDirectoryPath);
        }

        private IEnumerator TestDeleteActionDirectory(string testDirectory)
        {
            // Delete a directory with a DeleteAction

            // Directory1 <- delete
            //   file1.txt
            //   Directory2
            //     file2.txt
            // Directory1.txt <- keep
            // Directory3 <- keep


            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            // Get a reference to the file browser
            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            // Setup
            string directory1Name = "Directory1";
            string directory1Path = FileBrowserCore.CreateDirectory(parentPath:testDirectory, name:directory1Name);

            string file1Name = "file1.txt";
            string file1Text = "Test file 1";
            string file1Path = FileBrowserCore.CreateFile(directoryPath:directory1Path, name:file1Name, text:file1Text);

            string directory2Name = "Directory2";
            string directory2Path = FileBrowserCore.CreateDirectory(parentPath:directory1Path, name:directory2Name);

            string file2Name = "file2.txt";
            string file2Text = "Test file 2";
            string file2Path = FileBrowserCore.CreateFile(directoryPath:directory2Path, name:file2Name, text:file2Text);

            string fileDirectory1Name = "Directory1.txt";
            string fileDirectory1Text = "Test file Directory1";
            string fileDirectory1Path = FileBrowserCore.CreateFile(directoryPath:testDirectory, name:fileDirectory1Name, text:fileDirectory1Text);

            string directory3Name = "Directory3";
            string directory3Path = FileBrowserCore.CreateDirectory(parentPath:testDirectory, name:directory3Name);


            fb.Open(initialPath:testDirectory);

            bool deleteActionFinished = false;

            fb.OnActionListChangedEvent += () => {
                if(fb.Actions.Count > 0 && fb.Actions[0] is FileBrowserDeleteAction deleteAction){
                    deleteAction.OnFinishedEvent += (FileBrowserDeleteAction action) => {

                        // Check Directory1 was deleted
                        Assert.False(FileBrowserCore.DirectoryExists(path:directory1Path));

                        // Check other files and directories still exist

                        string fileDirectory1TextAfterDelete = FileBrowserCore.ReadTextFromFile(filePath:fileDirectory1Path);
                        Assert.AreEqual(fileDirectory1Text, fileDirectory1TextAfterDelete);

                        Assert.True(FileBrowserCore.DirectoryExists(path:directory3Path));

                        deleteActionFinished = true;
                    };

                    deleteAction.Start();
                }
            };

            FileSystemEntry[] fileSystemEntries = FileBrowserCore.GetFileSystemEntriesInDirectory(path:testDirectory);

            for(int i = 0; i < fileSystemEntries.Length; i++){
                if(fileSystemEntries[i].Name == directory1Name){
                    
                    fb.DeleteFileSystemEntry(fileSystemEntry:fileSystemEntries[i]);

                    break;
                }
            }

            
            yield return new WaitWhile(() => deleteActionFinished == false);
        }



        private IEnumerator GetSAFTestDirectory(Action<string> callback){
            bool sceneLoaded = false;
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => sceneLoaded = true;
            SceneManager.LoadScene("TestFileBrowser");

            yield return new WaitWhile(() => sceneLoaded == false);

            FileBrowserCore fb = GameObject.FindObjectOfType<FileBrowserCore>();

            fb.Open();

            if (!FileBrowserCore.UseSAF){
                Assert.Ignore("This test is only executed when using SAF");
            }

#if UNITY_ANDROID

            // Create a temporary directory to test in

            string testDirectoryName = "Test Directory";
            string testDirectoryPath = null;

            bool safTestFolderLoaded = false;

            // Try to create the directory
            for(int i = 0; i < fb.Shortcuts.Count; i++){
                if(fb.Shortcuts[i].Name.Equals("AcobsFBTest")){
                    testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:fb.Shortcuts[i].TargetPath, name:testDirectoryName);
                    safTestFolderLoaded = true;
                    break;
                }
            } 


            if(string.IsNullOrEmpty(testDirectoryPath)){
                // Request permission to create the folder

                Debug.Log("To execute SAF tests a folder called AcobsFBTest must be created and added as a shortcut.");

                fb.OnShortcutsChangedEvent += () => {
                    
                    for(int i = 0; i < fb.Shortcuts.Count; i++){
                        if(fb.Shortcuts[i].Name.Equals("AcobsFBTest")){
                            testDirectoryPath = FileBrowserCore.CreateDirectory(parentPath:fb.Shortcuts[i].TargetPath, name:testDirectoryName);
                            break;
                        }
                    } 

                    safTestFolderLoaded = true;
                };

                fb.AddShortcutSAF();
            }

            yield return new WaitWhile(() => safTestFolderLoaded == false);

            Assert.False(string.IsNullOrEmpty(testDirectoryPath));

            fb.Close();

            callback(testDirectoryPath);
#else
            Assert.Fail("Not running on Android");
#endif
        }

        private string GetDataPath(){
            string dataPath = "";
#if UNITY_EDITOR
			dataPath = Application.dataPath;
#else
			dataPath = Application.persistentDataPath;
#endif
            return dataPath;
        }
    }
}
