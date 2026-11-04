import 'package:flutter/material.dart';

import '../../models/vehicle.dart';

/// One document slot (PDF or photo). File picking/upload is not wired yet;
/// [onAttach] currently just fakes a file name so the UI can be reviewed.
class AttachmentTile extends StatelessWidget {
  const AttachmentTile({super.key, required this.document, required this.onAttach, required this.onRemove});

  final VehicleDocument document;
  final VoidCallback onAttach;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final kind = document.kind;
    final uploaded = document.isUploaded;

    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: CircleAvatar(
        backgroundColor: uploaded ? scheme.primaryContainer : scheme.surfaceContainerHighest,
        child: Icon(
          kind.isPhoto ? Icons.photo_camera_outlined : Icons.picture_as_pdf_outlined,
          color: uploaded ? scheme.onPrimaryContainer : scheme.onSurfaceVariant,
        ),
      ),
      title: Text(kind.label),
      subtitle: Text(uploaded ? document.fileName! : kind.hint),
      trailing: uploaded
          ? IconButton(
              tooltip: 'Remove ${kind.label}',
              icon: const Icon(Icons.close),
              onPressed: onRemove,
            )
          : TextButton.icon(
              onPressed: onAttach,
              icon: Icon(kind.isPhoto ? Icons.add_a_photo_outlined : Icons.upload_file),
              label: Text(kind.isPhoto ? 'Add photo' : 'Attach'),
            ),
    );
  }
}
