from railroader import ui, enusdeRecorderCommand, enusdeDiagnosticWindow, enusdeGrenzzeichen

// The native window owns stable, periodically updated text fields.
// No Relay coroutine and no periodically rebuilt scroll view.
func onWorldLoad(ctx) {
    ui.registerCommand("enus_signal", "Signaltechnik-Diagnose öffnen oder schließen", (args) => {
        return enusdeDiagnosticWindow("toggle")
    })
    ui.registerCommand("enus_signal_status", "Diagnosestatus ausgeben", (args) => {
        return enusdeDiagnosticWindow("status")
    })
    ui.registerCommand("enus_signal_record", "Hintergrundaufzeichnung: status, an oder aus", (args) => {
        if length(args) > 0 { return enusdeRecorderCommand(args[0]) }
        return enusdeRecorderCommand("status")
    })
    ui.registerCommand("enus_grenzzeichen", "Lokales Grenzzeichen: Vorschau, setzen, abbrechen, Status", (args) => {
        if length(args) > 0 { return enusdeGrenzzeichen(args[0]) }
        return enusdeGrenzzeichen("open")
    })
    print("[EnusDE Signaltechnik] Konsolenbefehle bereit: /enus_signal und /enus_grenzzeichen.")
}
func onWorldUnload(ctx) {
    // Native lifecycle removes the owned window and stops the recorder.
}
