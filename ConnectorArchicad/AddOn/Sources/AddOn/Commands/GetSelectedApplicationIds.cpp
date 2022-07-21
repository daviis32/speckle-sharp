#include "GetSelectedApplicationIds.hpp"
#include "ResourceIds.hpp"
#include "ObjectState.hpp"
#include "Utility.hpp"
#include "FieldNames.hpp"

#include "MemoryMapped.hpp"


namespace AddOnCommands {

  GS::Array<API_Guid> GetSelectedApplicationIds::GetSelectedElementGuids()
  {
    GS::Array<API_Guid>		elementGuids;
    API_SelectionInfo		selectionInfo;
    GS::Array<API_Neig>		selNeigs;

    GSErrCode err = ACAPI_Selection_Get(&selectionInfo, &selNeigs, true);
    if (err == NoError) {
        if (selectionInfo.typeID != API_SelEmpty) {
            for (const API_Neig& neig : selNeigs) {
                if (Utility::IsElement3D(neig.guid)) {
                    elementGuids.Push(neig.guid);
                }
            }
        }
    }
   
    BMKillHandle((GSHandle*)&selectionInfo.marquee.coords);

    return elementGuids;
  }


  GS::String GetSelectedApplicationIds::GetName() const
  {
    return GetSelectedApplicationIdsCommandName;
  }

  GS::ObjectState GetSelectedApplicationIds::Execute(const GS::ObjectState& /*parameters*/, GS::ProcessControl& /*processControl*/) const
  {
    GS::Array<API_Guid>	elementGuids = GetSelectedElementGuids();

    GS::ObjectState retVal;

    const auto& listAdder = retVal.AddList<GS::UniString>(ApplicationIdsFieldName);
    for (const API_Guid& guid : elementGuids) {
      listAdder(APIGuidToString(guid));
    }

    return retVal;
  }



  GS::Array<API_Guid> Cmd_GetSelectedApplicationIds::GetSelectedElementGuids()
  {
      GS::Array<API_Guid>		elementGuids;
      API_SelectionInfo		selectionInfo;
      GS::Array<API_Neig>		selNeigs;

      GSErrCode err = ACAPI_Selection_Get(&selectionInfo, &selNeigs, true);
      if (err == NoError) {
          if (selectionInfo.typeID != API_SelEmpty) {
              for (const API_Neig& neig : selNeigs) {
                  if (Utility::IsElement3D(neig.guid)) {
                      elementGuids.Push(neig.guid);
                  }
              }
          }
      }

      BMKillHandle((GSHandle*)&selectionInfo.marquee.coords);

      return elementGuids;
  }

  bool Cmd_GetSelectedApplicationIds::Execute(const rapidjson::Value&, rapidjson::Document& output)
  {
	  using namespace rapidjson;

	  GS::Array<API_Guid> list = GetSelectedElementGuids();

	  Document::AllocatorType& allocator = output.GetAllocator();

	  output.AddMember("succeeded", Value().SetBool(true), allocator);

	  Value addonCmdresponse(kObjectType);

	  Value applicationIds(kArrayType);
	  for (const API_Guid& guid : list) {
		  const char* val = static_cast<const char*>(APIGuidToString(guid).ToCStr());
		  Value id;
		  id.SetString(val, allocator);
		  applicationIds.PushBack(id, allocator);
	  }

	  addonCmdresponse.AddMember("applicationIds", applicationIds, allocator);

	  Value result(kObjectType);
	  result.AddMember("addOnCommandResponse", addonCmdresponse, allocator);
	  output.AddMember("result", result, allocator);

	  return true;
  }

  std::string Cmd_GetSelectedApplicationIds::GetName() const
  {
      return GetSelectedApplicationIdsCommandName;
  }
}