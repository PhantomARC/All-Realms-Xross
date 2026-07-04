// TODO: Update to match your plugin's package name.
package uarx.allrealmsxross.arxnfc

import android.nfc.NfcAdapter
import android.nfc.Tag
import android.nfc.tech.MifareUltralight
import android.util.Log
import org.godotengine.godot.Godot
import org.godotengine.godot.plugin.GodotPlugin
import org.godotengine.godot.plugin.SignalInfo
import org.godotengine.godot.plugin.UsedByGodot
import java.io.IOException

class GodotAndroidPlugin(godot: Godot) : GodotPlugin(godot), NfcAdapter.ReaderCallback {
    private enum class TAGState {
        DETECT,
        UNLOCKED,
        LOCKED,
        BAD_PACK,
        PSWD_FAIL
    }
    private enum class OP {
        READ,
        WRITE,
        SIGN
    }

    private val PASSWORD = byteArrayOf(0x5F, 0x41, 0x52, 0x58)
    private val ACK_BYTES = byteArrayOf(0x02,0x47)
    private val SIGNATURE = listOf(
        Pair(229,  byteArrayOf(0x5F, 0x41, 0x52, 0x58)),
        Pair(230,  byteArrayOf(0x02, 0x47, 0x00, 0x00)),
        Pair(227, byteArrayOf(0x00, 0x00, 0x00, 0x00)),
        Pair(228, byteArrayOf(0xC0.toByte(), 0x00, 0x00, 0x00))
    )

    private var rNFC: NfcAdapter? = null
    private var currTag: MifareUltralight? = null
    private var isNFC = false //toggle listen state
    private var opMode = OP.READ //operational status
    private var readIndex = 227
    private val writePayload = mutableListOf<Pair<Int, ByteArray>>()

    
    /* Retrieve plugin name */
    override fun getPluginName() = BuildConfig.GODOT_PLUGIN_NAME

    override fun getPluginSignals(): Set<SignalInfo> {
        return setOf(
            SignalInfo("tag_discovered", String::class.java, String::class.java),
            SignalInfo("tag_lost"),
            SignalInfo("tag_read", String::class.java),
            SignalInfo("nfc_error", String::class.java)
        )
    }

    override fun onMainPause() { // Handle Android interruptions
        super.onMainPause()
        rNFC?.disableReaderMode(activity)
    }

    override fun onMainResume() {
        super.onMainResume()
        if (isNFC) onNFC()
    }

    private fun closeTag() {
        currTag?.runCatching { close() }
        currTag = null
    }

    override fun onTagDiscovered(tag: Tag) {
        closeTag() //purge old tag
        
        val mifare = MifareUltralight.get(tag)
        if (mifare == null) {
            emitSignal("nfc_error", "Tag is not MifareUltralight compatible.")
            return
        }
        var tagType = TAGState.DETECT //defaults to failed card

        try {
            mifare.connect()
            currTag = mifare
            mifare.timeout = 500

            var authPack: ByteArray? = null 
            try { 
                // Attempt to read page 227 (0xE3)
                authPack = mifare.readPages(227) 
            } catch (e: IOException) { 
                // [FIX] FULL_LOCK: Read failed due to password protection (NAK).
                // We MUST reconnect to re-establish the severed RF session.
                mifare.close()
                mifare.connect()
            } 

            if (authPack != null && authPack.size >= 4) {
                val authPage = authPack.copyOfRange(0, 4)
                val targetPage = byteArrayOf(0x04, 0x00, 0x00, 0xFF.toByte())
                
                if (authPage.contentEquals(targetPage)) tagType = TAGState.UNLOCKED
            } 
            
            if (tagType != TAGState.UNLOCKED) {
                try { 
                    // 0x1B is PSWD Auth CMD
                    val pack = mifare.transceive(byteArrayOf(0x1B) + PASSWORD)
                    if (pack == null || !pack.contentEquals(ACK_BYTES)) { //malformed PACK
                        tagType = TAGState.BAD_PACK
                        emitSignal("nfc_error", "Invalid PACK response.")
                    } else {
                        tagType = TAGState.LOCKED
                    }
                } catch (authException: IOException) { 
                    tagType = TAGState.PSWD_FAIL
                    emitSignal("nfc_error", "Failed authentication.")
                } // rejected PSWD
            }
            
            val uid = tag.id.joinToString("") { "%02X".format(it) }
            emitSignal("tag_discovered", uid, tagType.name)

            if (tagType == TAGState.LOCKED || tagType == TAGState.UNLOCKED) {
                when (opMode) {
                    OP.WRITE -> {}
                    OP.SIGN -> {if(tagType == TAGState.UNLOCKED) signNFC(mifare)}
                    else ->  readNFC(mifare)
                }
            }

        } catch (e: IOException) {
            emitSignal("nfc_error", "Failed to connect to tag: ${e.message}")
        }
    }


    private fun readNFC(mf: MifareUltralight) {
        try {
            val data = mf.readPages(readIndex)
            emitSignal("tag_read", data.joinToString(",") { "%02X".format(it) })               
        } catch (e: IOException) {
            emitSignal("nfc_error", "Read failed: ${e.message}")
            closeTag()
            emitSignal("tag_lost")
        }
    }


    fun signNFC(mf: MifareUltralight) {
        try {
            mf.timeout = 2000 // High timeout for configuration/EEPROM sectors

            for (item in SIGNATURE) {
                mf.writePage(item.first, item.second)
                Thread.sleep(10)
            }
        } catch (e: IOException) {
            emitSignal("nfc_error", "Configuration write sequence failed: ${e.message}")
            closeTag()
            emitSignal("tag_lost")
        }
    }



    @UsedByGodot
    fun onNFC() { //turns the adapter on
        isNFC = true
        activity?.runOnUiThread {
            rNFC = NfcAdapter.getDefaultAdapter(activity)
            if (rNFC == null) {
                emitSignal("nfc_error", "NFC is not available on this device.")
                return@runOnUiThread
            }
            
            // FLAG_READER_SKIP_NDEF_CHECK is vital here to bypass NDEF processing
            val flags = NfcAdapter.FLAG_READER_NFC_A or 
                NfcAdapter.FLAG_READER_SKIP_NDEF_CHECK or 
                NfcAdapter.FLAG_READER_NO_PLATFORM_SOUNDS
            rNFC?.enableReaderMode(activity, this, flags, null)
        }
    }

    @UsedByGodot
    fun offNFC() { //turns the adapter off
        isNFC = false
        activity?.runOnUiThread {
            rNFC?.disableReaderMode(activity)
            closeTag()
        }
    }

    @UsedByGodot
    fun read(pageIndex: Int): ByteArray {
        val mifare = currTag
        if (mifare == null || !mifare.isConnected) {
            emitSignal("nfc_error", "No tag connected.")
            return ByteArray(0)
        }

        return try {
            mifare.readPages(pageIndex)
        } catch (e: IOException) {
            emitSignal("nfc_error", "Read failed: ${e.message}")
            closeTag()
            emitSignal("tag_lost")
            ByteArray(0)
        }
    }

    @UsedByGodot
    fun write(pageIndex: Int, data: ByteArray): Boolean {
        // Enforce the 4-byte strictness 
        if (data.size != 4) {
            emitSignal("nfc_error", "Data array must be exactly 4 bytes.")
            return false
        }

        val mifare = currTag
        if (mifare == null || !mifare.isConnected) {
            emitSignal("nfc_error", "No tag connected.")
            return false
        }

        return try {
            mifare.writePage(pageIndex, data)
            true
        } catch (e: IOException) {
            emitSignal("nfc_error", "Write failed: ${e.message}")
            closeTag()
            emitSignal("tag_lost")
            false
        }
    }

    @UsedByGodot
    fun setOpMode(mode: String) {
        opMode = when (mode) {
            "WRITE" -> OP.WRITE
            "SIGN"  -> OP.SIGN
            else    -> OP.READ
        }
    }
}