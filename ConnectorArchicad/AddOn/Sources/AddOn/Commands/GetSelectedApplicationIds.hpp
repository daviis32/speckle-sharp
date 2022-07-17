#ifndef GET_SELECTED_ELEMENT_IDS_HPP
#define GET_SELECTED_ELEMENT_IDS_HPP

#include "rapidjson/document.h"

#include "BaseCommand.hpp"


namespace AddOnCommands {


  class GetSelectedApplicationIds : public BaseCommand {
  public:
    static GS::Array<API_Guid>          GetSelectedElementGuids();
    virtual GS::String					GetName() const override;
    virtual GS::ObjectState				Execute(const GS::ObjectState& parameters, GS::ProcessControl& processControl) const override;
    static bool                         Execute(const rapidjson::Value& parameters, rapidjson::Document& output);
  };


}


#endif