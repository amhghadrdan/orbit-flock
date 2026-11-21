Target amd64-unicode
RequestExecutionLevel user
SetCompressor /SOLID lzma
CRCCheck force

!include "MUI2.nsh"
!include "FileFunc.nsh"

!define APP_NAME "Orbit Flock"
!define APP_VERSION "1.0.0"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\OrbitFlock"

!ifndef PLAYER_DIR
!define PLAYER_DIR "../Build/OrbitFlock-Windows"
!endif

!ifndef SETUP_FILE
!define SETUP_FILE "../Build/OrbitFlock-Setup-x64.exe"
!endif

Name "${APP_NAME}"
OutFile "${SETUP_FILE}"
InstallDir "$LOCALAPPDATA\Programs\Orbit Flock"
VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "${APP_NAME}"
VIAddVersionKey "FileDescription" "${APP_NAME} Setup"
VIAddVersionKey "FileVersion" "${APP_VERSION}"
VIAddVersionKey "ProductVersion" "${APP_VERSION}"
VIAddVersionKey "CompanyName" "${APP_NAME}"
VIAddVersionKey "LegalCopyright" "${APP_NAME}"

!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\OrbitFlock.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Launch Orbit Flock"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Section "Install"
    SetRegView 64
    SetShellVarContext current
    SetOutPath "$INSTDIR"
    File /r "${PLAYER_DIR}/*"
    SetOutPath "$INSTDIR"

    WriteUninstaller "$INSTDIR\Uninstall.exe"
    CreateDirectory "$SMPROGRAMS\Orbit Flock"
    CreateShortCut "$SMPROGRAMS\Orbit Flock\Orbit Flock.lnk" "$INSTDIR\OrbitFlock.exe"

    WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayName" "${APP_NAME}"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\OrbitFlock.exe"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "Publisher" "${APP_NAME}"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKCU "${UNINSTALL_KEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
    WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoModify" 1
    WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoRepair" 1
    ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
    WriteRegDWORD HKCU "${UNINSTALL_KEY}" "EstimatedSize" $0
SectionEnd

Section "Uninstall"
    SetRegView 64
    SetShellVarContext current

    Delete "$SMPROGRAMS\Orbit Flock\Orbit Flock.lnk"
    RMDir "$SMPROGRAMS\Orbit Flock"
    DeleteRegKey HKCU "${UNINSTALL_KEY}"

    Delete "$INSTDIR\OrbitFlock.exe"
    Delete "$INSTDIR\UnityPlayer.dll"
    Delete "$INSTDIR\UnityCrashHandler64.exe"
    RMDir /r "$INSTDIR\OrbitFlock_Data"
    RMDir /r "$INSTDIR\MonoBleedingEdge"
    Delete "$INSTDIR\Uninstall.exe"
    RMDir "$INSTDIR"
SectionEnd
