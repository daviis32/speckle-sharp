#ifndef ADDON_COMMAND_FACTORY_HPP
#define ADDON_COMMAND_FACTORY_HPP

#include "AddonCommand.hpp"

#include <string>
#include <unordered_map>

namespace AddOnCommands {


	class AddonCommandRegistry
	{
	private:
		std::unordered_map<std::string, AddonCommandRef> addonCommands;

	public:
		static AddonCommandRegistry& GetInstance();

	public:
		AddonCommandRef GetCommandByName(const std::string& commandName);
		void RegisterCommand(AddonCommandRef command);
	};



	inline AddonCommandRegistry& AddonCommandRegistry::GetInstance()
	{
		static AddonCommandRegistry instance;

		return instance;
	}

}


#endif