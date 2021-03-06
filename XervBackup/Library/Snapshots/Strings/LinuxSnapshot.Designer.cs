﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.237
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace XervBackup.Library.Snapshots.Strings {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class LinuxSnapshot {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal LinuxSnapshot() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("XervBackup.Library.Snapshots.Strings.LinuxSnapshot", typeof(LinuxSnapshot).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The external command failed to start.
        ///Error message: {0}
        ///Command: {1} {2}.
        /// </summary>
        internal static string ExternalProgramLaunchError {
            get {
                return ResourceManager.GetString("ExternalProgramLaunchError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The external command failed to complete within the set time limit: {0} {1}.
        /// </summary>
        internal static string ExternalProgramTimeoutError {
            get {
                return ResourceManager.GetString("ExternalProgramTimeoutError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to match local path {0} with any snapshot path: {1}.
        /// </summary>
        internal static string InvalidFilePathError {
            get {
                return ResourceManager.GetString("InvalidFilePathError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Script returned successfully, but the temporary folder {0} does not exist: {1}.
        /// </summary>
        internal static string MountFolderMissingError {
            get {
                return ResourceManager.GetString("MountFolderMissingError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Script returned successfully, but the temporary folder {0} still exist: {1}.
        /// </summary>
        internal static string MountFolderNotRemovedError {
            get {
                return ResourceManager.GetString("MountFolderNotRemovedError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The script returned exit code {0}, but {1} was expected: {2}.
        /// </summary>
        internal static string ScriptExitCodeError {
            get {
                return ResourceManager.GetString("ScriptExitCodeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Script returned successfully, but the output was missing the {0} parameter: {1}.
        /// </summary>
        internal static string ScriptOutputError {
            get {
                return ResourceManager.GetString("ScriptOutputError", resourceCulture);
            }
        }
    }
}
