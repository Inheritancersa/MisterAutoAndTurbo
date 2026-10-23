import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../data/car_makes.dart';
import '../models/vehicle.dart';
import '../theme.dart';
import '../utils/helpers.dart';
import '../viewmodels/workshop_viewmodel.dart';
import '../widgets/common.dart';

/// Register VehiclePhone (39:7)
///
/// Stateful only to own the text controllers and the focus node; the new
/// vehicle is saved in [WorkshopViewModel].
class RegisterVehicleView extends StatefulWidget {
  const RegisterVehicleView({super.key});
  @override
  State<RegisterVehicleView> createState() => _RegisterVehicleViewState();
}

class _RegisterVehicleViewState extends State<RegisterVehicleView> {
  final _form = GlobalKey<FormState>();
  final _make = TextEditingController();
  final _makeFocus = FocusNode();
  final _model = TextEditingController();
  final _plate = TextEditingController();
  final _year = TextEditingController();
  final _mileage = TextEditingController();

  @override
  void dispose() {
    for (final c in [_make, _model, _plate, _year, _mileage]) {
      c.dispose();
    }
    _makeFocus.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_form.currentState!.validate()) return;
    final error = await context.read<WorkshopViewModel>().addVehicle(Vehicle(
          id: 'v${DateTime.now().millisecondsSinceEpoch}',
          make: exactMake(_make.text) ?? _make.text.trim(),
          model: _model.text.trim(),
          plate: _plate.text.trim().toUpperCase(),
          year: int.parse(_year.text.trim()),
          mileage: int.tryParse(_mileage.text.trim()) ?? 0,
        ));
    if (!mounted) return;
    if (error != null) {
      snack(context, error);
      return;
    }
    snack(context, 'Vehicle saved');
    Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    final label = AppText.inter(18, w: FontWeight.w700, c: Colors.black);
    return AppScreen(
      title: 'Register Your Vehicle.',
      children: [
        Form(
          key: _form,
          child: Column(children: [
            _MakeField(controller: _make, focusNode: _makeFocus, labelStyle: label),
            LabeledField(label: 'MODEL', labelStyle: label, controller: _model, validator: requiredField),
            LabeledField(label: 'Registration Plate', labelStyle: label, controller: _plate, validator: requiredField),
            LabeledField(
              label: 'Year',
              labelStyle: label,
              controller: _year,
              keyboard: TextInputType.number,
              inputFormatters: [digitsOnlyFormatter],
              validator: (v) {
                final y = int.tryParse(v ?? '');
                return (y == null || y < 1950 || y > DateTime.now().year + 1) ? 'Enter a valid year' : null;
              },
            ),
            LabeledField(
              label: 'Mileage (km)',
              labelStyle: label,
              controller: _mileage,
              keyboard: TextInputType.number,
              inputFormatters: [digitsOnlyFormatter],
              validator: (v) => digitsField(v, min: 0, max: 2000000, label: 'Mileage'),
            ),
          ]),
        ),
        const SizedBox(height: 10),
        WideButton('Save Vehicle Details', green: true, onPressed: _save),
      ],
    );
  }
}

/// MAKE field: type the first letters (e.g. "to") and matching brands appear
/// in a list to tap. Only a make from the list is accepted.
class _MakeField extends StatelessWidget {
  final TextEditingController controller;
  final FocusNode focusNode;
  final TextStyle labelStyle;
  const _MakeField({required this.controller, required this.focusNode, required this.labelStyle});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 14),
        child: LayoutBuilder(
          builder: (context, constraints) => Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('MAKE', style: labelStyle),
              const SizedBox(height: 8),
              RawAutocomplete<String>(
                textEditingController: controller,
                focusNode: focusNode,
                optionsBuilder: (value) => searchMakes(value.text),
                onSelected: (_) => focusNode.unfocus(),
                fieldViewBuilder: (context, textController, node, onSubmit) => TextFormField(
                  controller: textController,
                  focusNode: node,
                  textCapitalization: TextCapitalization.words,
                  style: AppText.inter(16, w: FontWeight.w500),
                  validator: (v) {
                    if (v == null || v.trim().isEmpty) return 'Required';
                    return exactMake(v) == null ? 'Please choose a make from the list' : null;
                  },
                  decoration: fieldDecoration(
                    hintText: 'Type to search, e.g. "to" for Toyota',
                    hintStyle: AppText.inter(16, w: FontWeight.w500, c: AppColors.label),
                    suffixIcon: const Icon(Icons.keyboard_arrow_down, color: Colors.black),
                  ),
                ),
                optionsViewBuilder: (context, onSelected, options) => Align(
                  alignment: Alignment.topLeft,
                  child: Material(
                    elevation: 4,
                    borderRadius: BorderRadius.circular(8),
                    color: Colors.white,
                    child: ConstrainedBox(
                      constraints: BoxConstraints(maxHeight: 240, maxWidth: constraints.maxWidth),
                      child: ListView.builder(
                        padding: EdgeInsets.zero,
                        shrinkWrap: true,
                        itemCount: options.length,
                        itemBuilder: (context, i) {
                          final option = options.elementAt(i);
                          return InkWell(
                            onTap: () => onSelected(option),
                            child: Padding(
                              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                              child: SizedBox(
                                width: double.infinity,
                                child: Text(option, style: AppText.inter(16, w: FontWeight.w500)),
                              ),
                            ),
                          );
                        },
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      );
}
