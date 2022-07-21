#include "AddonCommandRegistry.hpp"

#include <string>

namespace AddOnCommands {


	AddonCommandRef AddonCommandRegistry::GetCommandByName(const std::string& commandName)
	{
		auto cmdPair = addonCommands.find(commandName);

		if (cmdPair != addonCommands.end()) {
			return cmdPair->second;
		}

		return nullptr;
	}

	void AddonCommandRegistry::RegisterCommand(AddonCommandRef command)
	{
		if (command != nullptr && command->GetName().length() > 0) {
			addonCommands.insert(std::pair<std::string, AddonCommandRef>(command->GetName(), command));
		}
	}
}
