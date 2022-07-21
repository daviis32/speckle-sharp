#ifndef ADDON_COMMAND_HPP
#define ADDON_COMMAND_HPP

#include "rapidjson/document.h"

#include <string>
#include <memory>

namespace AddOnCommands {


	class AddonCommand {
	public:
		virtual std::string GetName() const = 0;
		virtual bool Execute(const rapidjson::Value&, rapidjson::Document& output) = 0;
	};

	typedef std::shared_ptr<AddonCommand> AddonCommandRef;
}


#endif