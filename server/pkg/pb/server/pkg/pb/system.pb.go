
package pb

import (
	protoreflect "google.golang.org/protobuf/reflect/protoreflect"
	protoimpl "google.golang.org/protobuf/runtime/protoimpl"
	reflect "reflect"
	sync "sync"
	unsafe "unsafe"
)

const (
	_ = protoimpl.EnforceVersion(20 - protoimpl.MinVersion)
	_ = protoimpl.EnforceVersion(protoimpl.MaxVersion - 20)
)

type VersionConfig struct {
	state              protoimpl.MessageState `protogen:"open.v1"`
	ClientVersion      string                 `protobuf:"bytes,1,opt,name=client_version,json=clientVersion,proto3" json:"client_version,omitempty"`                // e.g. "1.0.5"
	AddressableVersion string                 `protobuf:"bytes,2,opt,name=addressable_version,json=addressableVersion,proto3" json:"addressable_version,omitempty"` // e.g. "2026.05.07.1"
	CatalogUrl         string                 `protobuf:"bytes,3,opt,name=catalog_url,json=catalogUrl,proto3" json:"catalog_url,omitempty"`                         // URL tới file .json catalog của Addressables
	ForceUpdate        bool                   `protobuf:"varint,4,opt,name=force_update,json=forceUpdate,proto3" json:"force_update,omitempty"`                     // Bắt buộc tải app mới từ Store
	UpdateDesc         string                 `protobuf:"bytes,5,opt,name=update_desc,json=updateDesc,proto3" json:"update_desc,omitempty"`                         // Nội dung cập nhật
	unknownFields      protoimpl.UnknownFields
	sizeCache          protoimpl.SizeCache
}

func (x *VersionConfig) Reset() {
	*x = VersionConfig{}
	mi := &file_system_proto_msgTypes[0]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *VersionConfig) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*VersionConfig) ProtoMessage() {}

func (x *VersionConfig) ProtoReflect() protoreflect.Message {
	mi := &file_system_proto_msgTypes[0]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*VersionConfig) Descriptor() ([]byte, []int) {
	return file_system_proto_rawDescGZIP(), []int{0}
}

func (x *VersionConfig) GetClientVersion() string {
	if x != nil {
		return x.ClientVersion
	}
	return ""
}

func (x *VersionConfig) GetAddressableVersion() string {
	if x != nil {
		return x.AddressableVersion
	}
	return ""
}

func (x *VersionConfig) GetCatalogUrl() string {
	if x != nil {
		return x.CatalogUrl
	}
	return ""
}

func (x *VersionConfig) GetForceUpdate() bool {
	if x != nil {
		return x.ForceUpdate
	}
	return false
}

func (x *VersionConfig) GetUpdateDesc() string {
	if x != nil {
		return x.UpdateDesc
	}
	return ""
}

type GetVersionRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Platform      string                 `protobuf:"bytes,1,opt,name=platform,proto3" json:"platform,omitempty"` // "android", "ios", "pc"
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetVersionRequest) Reset() {
	*x = GetVersionRequest{}
	mi := &file_system_proto_msgTypes[1]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetVersionRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetVersionRequest) ProtoMessage() {}

func (x *GetVersionRequest) ProtoReflect() protoreflect.Message {
	mi := &file_system_proto_msgTypes[1]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetVersionRequest) Descriptor() ([]byte, []int) {
	return file_system_proto_rawDescGZIP(), []int{1}
}

func (x *GetVersionRequest) GetPlatform() string {
	if x != nil {
		return x.Platform
	}
	return ""
}

type GetVersionResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Code          int32                  `protobuf:"varint,1,opt,name=code,proto3" json:"code,omitempty"`
	MessageId     string                 `protobuf:"bytes,2,opt,name=message_id,json=messageId,proto3" json:"message_id,omitempty"`
	Config        *VersionConfig         `protobuf:"bytes,3,opt,name=config,proto3" json:"config,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetVersionResponse) Reset() {
	*x = GetVersionResponse{}
	mi := &file_system_proto_msgTypes[2]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetVersionResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetVersionResponse) ProtoMessage() {}

func (x *GetVersionResponse) ProtoReflect() protoreflect.Message {
	mi := &file_system_proto_msgTypes[2]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetVersionResponse) Descriptor() ([]byte, []int) {
	return file_system_proto_rawDescGZIP(), []int{2}
}

func (x *GetVersionResponse) GetCode() int32 {
	if x != nil {
		return x.Code
	}
	return 0
}

func (x *GetVersionResponse) GetMessageId() string {
	if x != nil {
		return x.MessageId
	}
	return ""
}

func (x *GetVersionResponse) GetConfig() *VersionConfig {
	if x != nil {
		return x.Config
	}
	return nil
}

type CutsceneData struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Id            string                 `protobuf:"bytes,1,opt,name=id,proto3" json:"id,omitempty"`
	JsonContent   string                 `protobuf:"bytes,2,opt,name=json_content,json=jsonContent,proto3" json:"json_content,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *CutsceneData) Reset() {
	*x = CutsceneData{}
	mi := &file_system_proto_msgTypes[3]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *CutsceneData) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*CutsceneData) ProtoMessage() {}

func (x *CutsceneData) ProtoReflect() protoreflect.Message {
	mi := &file_system_proto_msgTypes[3]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*CutsceneData) Descriptor() ([]byte, []int) {
	return file_system_proto_rawDescGZIP(), []int{3}
}

func (x *CutsceneData) GetId() string {
	if x != nil {
		return x.Id
	}
	return ""
}

func (x *CutsceneData) GetJsonContent() string {
	if x != nil {
		return x.JsonContent
	}
	return ""
}

type ListCutscenesResponse struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Ids           []string               `protobuf:"bytes,1,rep,name=ids,proto3" json:"ids,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *ListCutscenesResponse) Reset() {
	*x = ListCutscenesResponse{}
	mi := &file_system_proto_msgTypes[4]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *ListCutscenesResponse) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*ListCutscenesResponse) ProtoMessage() {}

func (x *ListCutscenesResponse) ProtoReflect() protoreflect.Message {
	mi := &file_system_proto_msgTypes[4]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*ListCutscenesResponse) Descriptor() ([]byte, []int) {
	return file_system_proto_rawDescGZIP(), []int{4}
}

func (x *ListCutscenesResponse) GetIds() []string {
	if x != nil {
		return x.Ids
	}
	return nil
}

type SaveCutsceneRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Cutscene      *CutsceneData          `protobuf:"bytes,1,opt,name=cutscene,proto3" json:"cutscene,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *SaveCutsceneRequest) Reset() {
	*x = SaveCutsceneRequest{}
	mi := &file_system_proto_msgTypes[5]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *SaveCutsceneRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*SaveCutsceneRequest) ProtoMessage() {}

func (x *SaveCutsceneRequest) ProtoReflect() protoreflect.Message {
	mi := &file_system_proto_msgTypes[5]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*SaveCutsceneRequest) Descriptor() ([]byte, []int) {
	return file_system_proto_rawDescGZIP(), []int{5}
}

func (x *SaveCutsceneRequest) GetCutscene() *CutsceneData {
	if x != nil {
		return x.Cutscene
	}
	return nil
}

type GetCutsceneRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	Id            string                 `protobuf:"bytes,1,opt,name=id,proto3" json:"id,omitempty"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *GetCutsceneRequest) Reset() {
	*x = GetCutsceneRequest{}
	mi := &file_system_proto_msgTypes[6]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *GetCutsceneRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*GetCutsceneRequest) ProtoMessage() {}

func (x *GetCutsceneRequest) ProtoReflect() protoreflect.Message {
	mi := &file_system_proto_msgTypes[6]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*GetCutsceneRequest) Descriptor() ([]byte, []int) {
	return file_system_proto_rawDescGZIP(), []int{6}
}

func (x *GetCutsceneRequest) GetId() string {
	if x != nil {
		return x.Id
	}
	return ""
}

type EmptyRequest struct {
	state         protoimpl.MessageState `protogen:"open.v1"`
	unknownFields protoimpl.UnknownFields
	sizeCache     protoimpl.SizeCache
}

func (x *EmptyRequest) Reset() {
	*x = EmptyRequest{}
	mi := &file_system_proto_msgTypes[7]
	ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
	ms.StoreMessageInfo(mi)
}

func (x *EmptyRequest) String() string {
	return protoimpl.X.MessageStringOf(x)
}

func (*EmptyRequest) ProtoMessage() {}

func (x *EmptyRequest) ProtoReflect() protoreflect.Message {
	mi := &file_system_proto_msgTypes[7]
	if x != nil {
		ms := protoimpl.X.MessageStateOf(protoimpl.Pointer(x))
		if ms.LoadMessageInfo() == nil {
			ms.StoreMessageInfo(mi)
		}
		return ms
	}
	return mi.MessageOf(x)
}

func (*EmptyRequest) Descriptor() ([]byte, []int) {
	return file_system_proto_rawDescGZIP(), []int{7}
}

var File_system_proto protoreflect.FileDescriptor

const file_system_proto_rawDesc = "" +
	"\n" +
	"\fsystem.proto\x12\x02pb\x1a\fcommon.proto\"\xcc\x01\n" +
	"\rVersionConfig\x12%\n" +
	"\x0eclient_version\x18\x01 \x01(\tR\rclientVersion\x12/\n" +
	"\x13addressable_version\x18\x02 \x01(\tR\x12addressableVersion\x12\x1f\n" +
	"\vcatalog_url\x18\x03 \x01(\tR\n" +
	"catalogUrl\x12!\n" +
	"\fforce_update\x18\x04 \x01(\bR\vforceUpdate\x12\x1f\n" +
	"\vupdate_desc\x18\x05 \x01(\tR\n" +
	"updateDesc\"/\n" +
	"\x11GetVersionRequest\x12\x1a\n" +
	"\bplatform\x18\x01 \x01(\tR\bplatform\"r\n" +
	"\x12GetVersionResponse\x12\x12\n" +
	"\x04code\x18\x01 \x01(\x05R\x04code\x12\x1d\n" +
	"\n" +
	"message_id\x18\x02 \x01(\tR\tmessageId\x12)\n" +
	"\x06config\x18\x03 \x01(\v2\x11.pb.VersionConfigR\x06config\"A\n" +
	"\fCutsceneData\x12\x0e\n" +
	"\x02id\x18\x01 \x01(\tR\x02id\x12!\n" +
	"\fjson_content\x18\x02 \x01(\tR\vjsonContent\")\n" +
	"\x15ListCutscenesResponse\x12\x10\n" +
	"\x03ids\x18\x01 \x03(\tR\x03ids\"C\n" +
	"\x13SaveCutsceneRequest\x12,\n" +
	"\bcutscene\x18\x01 \x01(\v2\x10.pb.CutsceneDataR\bcutscene\"$\n" +
	"\x12GetCutsceneRequest\x12\x0e\n" +
	"\x02id\x18\x01 \x01(\tR\x02id\"\x0e\n" +
	"\fEmptyRequestB*Z\x10server/pkg/pb;pb\xaa\x02\x15GameClient.Network.Pbb\x06proto3"

var (
	file_system_proto_rawDescOnce sync.Once
	file_system_proto_rawDescData []byte
)

func file_system_proto_rawDescGZIP() []byte {
	file_system_proto_rawDescOnce.Do(func() {
		file_system_proto_rawDescData = protoimpl.X.CompressGZIP(unsafe.Slice(unsafe.StringData(file_system_proto_rawDesc), len(file_system_proto_rawDesc)))
	})
	return file_system_proto_rawDescData
}

var file_system_proto_msgTypes = make([]protoimpl.MessageInfo, 8)
var file_system_proto_goTypes = []any{
	(*VersionConfig)(nil),         // 0: pb.VersionConfig
	(*GetVersionRequest)(nil),     // 1: pb.GetVersionRequest
	(*GetVersionResponse)(nil),    // 2: pb.GetVersionResponse
	(*CutsceneData)(nil),          // 3: pb.CutsceneData
	(*ListCutscenesResponse)(nil), // 4: pb.ListCutscenesResponse
	(*SaveCutsceneRequest)(nil),   // 5: pb.SaveCutsceneRequest
	(*GetCutsceneRequest)(nil),    // 6: pb.GetCutsceneRequest
	(*EmptyRequest)(nil),          // 7: pb.EmptyRequest
}
var file_system_proto_depIdxs = []int32{
	0, // 0: pb.GetVersionResponse.config:type_name -> pb.VersionConfig
	3, // 1: pb.SaveCutsceneRequest.cutscene:type_name -> pb.CutsceneData
	2, // [2:2] is the sub-list for method output_type
	2, // [2:2] is the sub-list for method input_type
	2, // [2:2] is the sub-list for extension type_name
	2, // [2:2] is the sub-list for extension extendee
	0, // [0:2] is the sub-list for field type_name
}

func init() { file_system_proto_init() }
func file_system_proto_init() {
	if File_system_proto != nil {
		return
	}
	file_common_proto_init()
	type x struct{}
	out := protoimpl.TypeBuilder{
		File: protoimpl.DescBuilder{
			GoPackagePath: reflect.TypeOf(x{}).PkgPath(),
			RawDescriptor: unsafe.Slice(unsafe.StringData(file_system_proto_rawDesc), len(file_system_proto_rawDesc)),
			NumEnums:      0,
			NumMessages:   8,
			NumExtensions: 0,
			NumServices:   0,
		},
		GoTypes:           file_system_proto_goTypes,
		DependencyIndexes: file_system_proto_depIdxs,
		MessageInfos:      file_system_proto_msgTypes,
	}.Build()
	File_system_proto = out.File
	file_system_proto_goTypes = nil
	file_system_proto_depIdxs = nil
}
