#ifndef GET_WALL_DATA_HPP
#define GET_WALL_DATA_HPP

#include "rapidjson/document.h"

#include "BaseCommand.hpp"


namespace AddOnCommands {


	class GetWallData : public BaseCommand {
	public:
		virtual GS::String							GetName() const override;
		virtual GS::ObjectState						Execute(const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
		static bool Execute(const rapidjson::Value& parameters, rapidjson::Document& output);
	};


}


#endif