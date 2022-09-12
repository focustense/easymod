Scriptname FTEasyFollowerMCMScript extends SKI_ConfigBase  

Actor Property PlayerRef Auto

;Option IDs
int iExport

;State variables
bool shouldExportOutfit = true
bool shouldExportInventory = false
bool shouldExportNonPlayableInventory = false
bool shouldExportSpells = false
bool shouldExportOnlyBaseSpells = true

string exportName

Function ExportEasyFollowerData(string fileName)
	Debug.Trace("Export custom follower data")

	Debug.Trace("Read player info")
	ActorBase playerBase = PlayerRef.GetActorBase()
	string playerRace = GetFormIdentifier(playerBase.GetRace().GetFormId())
	int playerSex = playerBase.GetSex()
	int playerSkinTone = Game.GetTintMaskColor(6, 0)
	float playerHeight = playerBase.GetHeight()
	int armorFormIdentifiers = 0
	If (shouldExportOutfit)
		Debug.Trace("Get equipped armors")
		armorFormIdentifiers = GetEquippedArmors()
	EndIf
	int inventory = 0;
	If (shouldExportInventory)
		Debug.Trace("Get inventory items")
		inventory = GetInventoryItems()
	EndIf
	int spells = 0
	If (shouldExportSpells)
		Debug.Trace("Get spell list")
		spells = GetSpells()
	EndIf

	Debug.Trace("Build JSON output")
	int playerData = JMap.object()
	JMap.setStr(playerData, "race", playerRace)
	JMap.setInt(playerData, "sex", playerSex)
	JMap.setInt(playerData, "skinToneColor", playerSkinTone)
	JMap.setFlt(playerData, "height", playerHeight)
	If (armorFormIdentifiers != 0)
		JMap.setObj(playerData, "equipment", armorFormIdentifiers)
	EndIf
	If (inventory != 0)
		JMap.setObj(playerData, "inventory", inventory)
	EndIf
	If (spells != 0)
		JMap.setObj(playerData, "spells", spells)
	EndIf
	JValue.writeToFile(playerData, "data/easyfollower/exported/" + fileName + ".json")
EndFunction

int Function GetEquippedArmors() ;/ returns JArray object /;
	int ignoredSlots = ;/ FX01 /; 0x80000000 +  ;/ Decapitate /; 0x00200000 + ;/ DecapitateHead /; 0x00100000
	int formIdentifiers = JArray.object()
	int slot = 1
	While (slot < 0x80000000)
		Debug.Trace("Next slot: " + slot)
		If (Math.LogicalAnd(slot, ignoredSlots) == 0)
			Armor nextArmor = PlayerRef.GetWornForm(slot) as Armor
			If (nextArmor)
				Debug.Trace("Found armor in slot, form ID = " + nextArmor.GetFormId())
				JArray.addStr(formIdentifiers, GetFormIdentifier(nextArmor.GetFormId()))
			EndIf
		EndIf
		slot *= 2
	EndWhile
	return formIdentifiers
EndFunction

int Function GetInventoryItems() ;/ returns JArray object /;
	int inventory = JArray.object()
	int itemCount = PlayerRef.GetNumItems()
	int itemIndex = 0
	While (itemIndex < itemCount)
		int itemData = JMap.object()
		Form item = PlayerRef.GetNthForm(itemIndex)
		If (item.IsPlayable() || shouldExportNonPlayableInventory)
			JMap.setStr(itemData, "formIdentifier", GetFormIdentifier(item.GetFormId()))
			JMap.setInt(itemData, "count", PlayerRef.GetItemCount(item))
			JArray.addObj(inventory, itemData)
		EndIf
		itemIndex += 1
	EndWhile
	return inventory
EndFunction

int Function GetSpells() ;/ returns JArray object /;
	int formIdentifiers = JArray.object()
	ActorBase playerBase = PlayerRef.GetActorBase()
	int spellCount
	If (shouldExportOnlyBaseSpells)
		spellCount = playerBase.GetSpellCount()
	Else
		spellCount = PlayerRef.GetSpellCount()
	EndIf
	int spellIndex = 0
	While (spellIndex < spellCount)
		Spell nextSpell
		If (shouldExportOnlyBaseSpells)
			nextSpell = playerBase.GetNthSpell(spellIndex)
		Else
			nextSpell = PlayerRef.GetNthSpell(spellIndex)
		EndIf
		JArray.addStr(formIdentifiers, GetFormIdentifier(nextSpell.GetFormId()))
		spellIndex += 1
	EndWhile
	return formIdentifiers
EndFunction

Function SetStateEnabled(string stateName, bool enabled)
	int flags = OPTION_FLAG_NONE
	If (!enabled)
		flags = OPTION_FLAG_DISABLED
	EndIf
	SetOptionFlagsST(flags, ;/ noUpdate /; false, stateName)
EndFunction

string Function GetFormIdentifier(int formId)
	int modIndex = Math.RightShift(formId, 24)
	string localId
	string fileName
	If (modIndex == 0xfe)
		int lightModIndex = Math.RightShift(Math.LogicalAnd(0x00fff000, formId), 12)
		fileName = Game.GetLightModName(lightModIndex)
		localId = IntToHex(Math.LogicalAnd(formId, 0x00000fff), 24)
	Else
		fileName = Game.GetModName(modIndex)
		 localId = IntToHex(Math.LogicalAnd(formId, 0x00ffffff), 24)
	EndIf
	return fileName + "|" + localId
EndFunction

string Function IntToHex(int value, int bits)
	String result
	String digits = "0123456789ABCDEF"
	Int shifted = 0
	While (shifted < bits)
		result = StringUtil.GetNthChar(digits, Math.LogicalAnd(0xF, value)) + result
		value = Math.RightShift(value, 4)
		shifted += 4
	EndWhile
	return result
EndFunction

Event OnConfigInit()
EndEvent

Event OnConfigOpen()
	exportName = PlayerRef.GetBaseObject().GetName()
EndEvent

Event OnPageReset(string page)
	SetCursorFillMode(TOP_TO_BOTTOM)
	AddHeaderOption("Export")
	AddToggleOptionST("EXPORT_OUTFIT_TOGGLE", "Export outfit", shouldExportOutfit)
	AddToggleOptionST("EXPORT_INVENTORY_TOGGLE", "Export inventory", shouldExportInventory)
	AddToggleOptionST("EXPORT_NON_PLAYABLE_INVENTORY_TOGGLE", "Include non-playable items", shouldExportNonPlayableInventory, OPTION_FLAG_DISABLED)
	AddToggleOptionST("EXPORT_SPELLS_TOGGLE", "Export spells", shouldExportSpells)
	AddToggleOptionST("EXPORT_ONLY_BASE_SPELLS_TOGGLE", "Only base spells", shouldExportOnlyBaseSpells, OPTION_FLAG_DISABLED)
	iExport = AddInputOption("Export current character", exportName)
EndEvent

Event OnOptionInputOpen(int option)
	If (option == iExport)
		SetInputDialogStartText(exportName)
	EndIf
EndEvent

Event OnOptionInputAccept(int option, string value)
	If (option == iExport)
		Debug.Trace("Export started")
		exportName = value
		ExportEasyFollowerData(exportName)
		Debug.Trace("Export CharGen data")
		CharGen.SaveExternalCharacter(exportName)
		Debug.Trace("Export finished")
		Debug.MessageBox("Character data exported. To create a follower, run 'easyfollower.exe -f \"" + exportName + "\"'.")
	EndIf
EndEvent

State EXPORT_OUTFIT_TOGGLE
	Event OnSelectST()
		shouldExportOutfit = !shouldExportOutfit
		SetToggleOptionValueST(shouldExportOutfit)
	EndEvent

	Event OnDefaultST()
		shouldExportOutfit = true
		SetToggleOptionValueST(shouldExportOutfit)
	EndEvent

	Event OnHighlightST()
		SetInfoText("Current armor/clothes will be set up the NPC's Default Outfit.")
	EndEvent
EndState

State EXPORT_INVENTORY_TOGGLE
	Event OnSelectST()
		shouldExportInventory = !shouldExportInventory
		SetToggleOptionValueST(shouldExportInventory)
		SetStateEnabled("EXPORT_NON_PLAYABLE_INVENTORY_TOGGLE", shouldExportInventory)
	EndEvent

	Event OnDefaultST()
		shouldExportInventory = false
		SetToggleOptionValueST(shouldExportInventory)
		SetStateEnabled("EXPORT_NON_PLAYABLE_INVENTORY_TOGGLE", shouldExportInventory)
	EndEvent

	Event OnHighlightST()
		SetInfoText("NPC will be set up with all current player items (only base items, excluding tempering, custom enchantments, etc.)")
	EndEvent
EndState

State EXPORT_NON_PLAYABLE_INVENTORY_TOGGLE
	Event OnSelectST()
		shouldExportNonPlayableInventory = !shouldExportNonPlayableInventory
		SetToggleOptionValueST(shouldExportNonPlayableInventory)
	EndEvent

	Event OnDefaultST()
		shouldExportNonPlayableInventory = false
		SetToggleOptionValueST(shouldExportNonPlayableInventory)
	EndEvent

	Event OnHighlightST()
		SetInfoText("Include non-playable items with inventory. These are often dummy items added by mods, used to trigger certain behaviors.")
	EndEvent
EndState

State EXPORT_SPELLS_TOGGLE
	Event OnSelectST()
		shouldExportSpells = !shouldExportSpells
		SetToggleOptionValueST(shouldExportSpells)
		SetStateEnabled("EXPORT_ONLY_BASE_SPELLS_TOGGLE", shouldExportSpells)
	EndEvent

	Event OnDefaultST()
		shouldExportSpells = true
		SetToggleOptionValueST(shouldExportSpells)
		SetStateEnabled("EXPORT_ONLY_BASE_SPELLS_TOGGLE", shouldExportSpells)
	EndEvent

	Event OnHighlightST()
		SetInfoText("NPC will possess all current player spells.")
	EndEvent
EndState

State EXPORT_ONLY_BASE_SPELLS_TOGGLE
	Event OnSelectST()
		shouldExportOnlyBaseSpells = !shouldExportOnlyBaseSpells
		SetToggleOptionValueST(shouldExportOnlyBaseSpells)
	EndEvent

	Event OnDefaultST()
		shouldExportOnlyBaseSpells = true
		SetToggleOptionValueST(shouldExportOnlyBaseSpells)
	EndEvent

	Event OnHighlightST()
		SetInfoText("Include only the player's ActorBase spells. This filters out a lot of junk added by mod scripts and should normally be left enabled.")
	EndEvent
EndState