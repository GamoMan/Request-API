USE [eAGM-Request]
GO

/****** Object:  Table [dbo].[UploadFile]    Script Date: 1/15/2023 3:09:51 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UploadFile](
	[ID] [uniqueidentifier] NOT NULL,
	[FileName] [varchar](50) NULL,
	[ContentType] [varchar](50) NULL,
	[FileContent] [varbinary](max) NULL,
	[CreatedDate] [datetime] NULL,
 CONSTRAINT [PK_UploadFiles] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO